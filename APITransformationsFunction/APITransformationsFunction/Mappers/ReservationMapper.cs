using APITransformationsFunction.Model.Reservation.Create.Request;
using APITransformationsFunction.Model.Reservation.Create.Response;
using APITransformationsFunction.Model.Reservation.Get;
using APITransformationsFunction.Model.Reservation.Search;
using APITransformationsFunction.Model.Reservation.Shared;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GetBase = APITransformationsFunction.Model.Reservation.Shared.Base;
using GetFee = APITransformationsFunction.Model.Reservation.Shared.Fee;
using GetGuarantee = APITransformationsFunction.Model.Reservation.Shared.Guarantee;
using GetGuestCount = APITransformationsFunction.Model.Reservation.Shared.GuestCount;
using GetRateDetail = APITransformationsFunction.Model.Reservation.Shared.RateDetail;
using GetRatePlan = APITransformationsFunction.Model.Reservation.Shared.RatePlan;
using GetRoomRate = APITransformationsFunction.Model.Reservation.Shared.RoomRate;
using GetSpecialRequest = APITransformationsFunction.Model.Reservation.Shared.SpecialRequest;
using GetTax = APITransformationsFunction.Model.Reservation.Shared.Tax;
using GetTotal = APITransformationsFunction.Model.Reservation.Shared.Total;
using GetUnitStay = APITransformationsFunction.Model.Reservation.Shared.UnitStay;
using GetUnitStayDates = APITransformationsFunction.Model.Reservation.Shared.UnitStayDates;

namespace APITransformationsFunction.Mappers
{
    public static class ReservationMapper
    {
        public static OTAGetReservationResponse MapToOTAResponse(OnexReservationRequest source)
        {
            if (source == null)
            {
                return new OTAGetReservationResponse
                {
                    CorrelationID = string.Empty,
                    TradingAgreementCode = "HGV-ARV-HGV",
                    MembershipClub = string.Empty,
                    MembershipClubLevel = string.Empty,
                    Reservations = []
                };
            }

            var ota = new OTAGetReservationResponse
            {
                CorrelationID = Guid.NewGuid().ToString(),
                TradingAgreementCode = "HGV-ARV-HGV",
                MembershipClub = "HGVClub",
                MembershipClubLevel = "Elite",
                Reservations = MapReservations(source)
            };

            return ota;
        }
        public static OTACreateReservationResponse MapOnexToOtaResponse(ReservationResponse onexResponse, OTACreateReservationRequest otaRequest)
        {
            if (onexResponse?.Data == null)
            {
                return null;
            }

            var bookingStatus = onexResponse.Data.BookingStatus?.Trim()?.ToUpperInvariant();
            var isSuccess = bookingStatus == "CONFIRMED";

            var otaResponse = new OTACreateReservationResponse
            {
                CorrelationID = otaRequest.CorrelationID,
                TradingAgreementCode = $"{onexResponse.Data.SystemMeta.TenantID}-ARV-{onexResponse.Data.SystemMeta.TenantID}",
                Reservations = []
            };

            foreach (var item in onexResponse.Data.BookingItems)
            {
                var reservation = new ReservationPostDetails
                {
                    PropertyCode = item.ParentCode,
                    UnitTypeCode = item.ItemCode,
                    Status = isSuccess ? bookingStatus : null,
                    Message = isSuccess
                        ? "Reservation created successfully"
                        : "Error while creating reservation",

                    SourceReservationIds = []
                };

                // Add EXCHANGE reference only on success
                if (isSuccess)
                {
                    reservation.CreateDateTime = onexResponse.Data.CreatedAt.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");

                    reservation.SourceReservationIds.Add(new SourceReservationId
                    {
                        SourceSystem = "EXCHANGE",
                        Id = onexResponse.Data.BookingId ?? string.Empty
                    });
                }

                // Always add PARTNER reference
                reservation.SourceReservationIds.Add(new SourceReservationId
                {
                    SourceSystem = onexResponse.Data.ExternalReference?.SourceSystem ?? "PARTNER",
                    Id = onexResponse.Data.ExternalReference?.SourceReferenceId ?? string.Empty
                });

                // Errors - only when reservation failed
                if (!isSuccess && onexResponse.Data.Error != null)
                {
                    reservation.Errors =
                    [
                        new ErrorDetails
                        {
                            Type = "https://api.hgv.com/errors/warning",
                            Code = "PARTIAL_SUCCESS",
                            Status = 201,
                            ShortText = onexResponse.Data.Error.ErrorMessage
                        }
                    ];
                }

                otaResponse.Reservations.Add(reservation);
            }

            return otaResponse;
        }
        public static OTASearchReservationResponse MapSearchOnexToOtaResponse(OnexSearchReservationResponse source)
        {
            if (source == null)
            {
                return new OTASearchReservationResponse
                {
                    CorrelationID = string.Empty,
                    TradingAgreementCode = "HGV-ARV-HGV",
                    MembershipClub = string.Empty,
                    MembershipClubLevel = string.Empty,
                    Reservations = []
                };
            }

            var ota = new OTASearchReservationResponse
            {
                CorrelationID = source.Id,
                TradingAgreementCode = $"{source.Data.SystemMeta.TenantID}-ARV-{source.Data.SystemMeta.TenantID}",
                MembershipClub = "HGVClub", // Check
                MembershipClubLevel = "Elite",
                Pagination = new Pagination
                {
                    Page = source.Data.Pagination.Page,
                    PageSize = source.Data.Pagination.PageSize,
                    TotalItems = source.Data.Pagination.TotalItems,
                    TotalPages = source.Data.Pagination.TotalPages
                },
                Reservations = MapSearchReservations(source)
            };

            return ota;
        }
        public static OnexReservationRequest MapToOnexRequest(OTACreateReservationRequest ota)
        {
            if (ota == null)
            {
                return null;
            }

            var src = ota?.ReservationCandidates?
              .SourceReservationIds?
              .FirstOrDefault(x =>
                  !string.IsNullOrWhiteSpace(x?.SourceSystem) &&
                  x.SourceSystem.Equals("PARTNER", StringComparison.OrdinalIgnoreCase));
            var onexResult = new OnexReservationRequest
            {
                Id = ota.CorrelationID,
                Source = "OneXChange",
                Specversion = "1.0",
                Type = "Reservation-Request",
                Datacontenttype = "application/json",
                Time = DateTime.UtcNow,
                Dataschema = "https://schemas.onetruth.example/reservations/request/2019-09",
                Subject = "create new reservation",
                Ispartial = false,
                Data = new ReservationRequestData
                {
                    SystemMeta = new Model.Reservation.Create.Request.SystemMeta
                    {
                        TenantID = "HGV",
                        InsertedBy = "api",
                        InsertedDateTime = DateTime.UtcNow,
                        UpdatedBy = "api",
                        UpdatedDateTime = DateTime.UtcNow,
                        Status = true,
                        StatusReason = "Initiated"
                    },
                    ExternalReference = new ExternalReference
                    {
                        SourceSystem = src?.SourceSystem ?? string.Empty,
                        SourceReferenceId = src?.Id ?? string.Empty,
                        TargetSystem = "HGV",
                        TargetReferenceId = "HGV"
                    },
                    BookingItems = MapOnexBookingItems(ota)

                }
            };
            return onexResult;
        }
        public static OnexSearchReservationRequest MapToSearchOnexRequest(OTASearchReservationRequest ota)
        {
            if (ota == null)
            {
                return null;
            }

            var onexResult = new OnexSearchReservationRequest
            {
                Id = Guid.NewGuid().ToString(),
                CorrelationId = ota.CorrelationID,
                Ispartial = false,
                Specversion = "1.0",
                Datacontenttype = "application/json",
                Dataschema = "http://json-schema.org/draft-07/schema#",
                Subject = "ReservationSearchRequest",
                Source = "OneXChange",
                Time = DateTime.UtcNow,
                Type = "Reservation-Search-Request",
                Data = new ReservationSearchData
                {
                    SystemMeta = new Model.Reservation.Search.SystemMeta
                    {
                        TenantID = "HGV",
                        InsertedBy = "api",
                        InsertedDateTime = DateTime.UtcNow,
                        UpdatedBy = "api",
                        UpdatedDateTime = DateTime.UtcNow
                    },
                    SearchCriteria = new SearchCriteria
                    {
                        TradingAgreementCode = ota.TradingAgreementCode,
                        Status = ota.Status,
                        ReservationDate = DateTime.TryParseExact(ota.ReservationDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var resDate) ? resDate : null,
                        ReservationDateInterval = ota.ReservationDateInterval,
                        ArrivalDate = DateTime.TryParseExact(ota.ArrivalDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var arrDate) ? arrDate : null,
                        ArrivalDateInterval = ota.ArrivalDateInterval,

                        // SourceReservationIds mapping
                        SourceReservationIds = ota.SourceReservationIds?.Select(x => new SourceIdentifier
                        {
                            SourceSystem = x.SourceSystem,
                            Id = x.Id
                        }).ToList(),

                        // SourceMemberIds mapping
                        SourceMemberIds = ota.SourceMemberIds?.Select(x => new SourceIdentifier
                        {
                            SourceSystem = x.SourceSystem,
                            Id = x.Id
                        }).ToList(),

                        // OrderBy mapping
                        OrderBy = ota.OrderBy != null
                        ? new OrderBy
                        {
                            Field = ota.OrderBy.Field,
                            Direction = ota.OrderBy.Direction
                        }
                        : null
                    }
                }

            };
            return onexResult;

        }
        private static List<ReservationGetDetails> MapReservations(OnexReservationRequest source)
        {
            var list = new List<ReservationGetDetails>();

            if (source.Data?.BookingItems != null && source.Data.BookingItems.Count != 0)
            {
                foreach (var bi in source.Data.BookingItems)
                {
                    var reservation = new ReservationGetDetails
                    {
                        // reservation-level fields
                        Status = string.Empty,
                        ReservationDate = bi?.ReservationDate?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        ArrivalDate = bi?.StartDate.ToString("yyyy-MM-dd"),
                        DepartureDate = bi?.EndDate.ToString("yyyy-MM-dd"),
                        SourceReservationIds =
                        [
                            new SourceReservationId
                            {
                                SourceSystem = source.Data?.ExternalReference?.SourceSystem ?? string.Empty,
                                Id = source.Data?.ExternalReference?.SourceReferenceId ?? string.Empty
                            },
                            new SourceReservationId
                            {
                                SourceSystem = "EXCHANGE",
                                Id = bi?.MemberID ?? string.Empty
                            }
                        ],

                        CancellationInfo = new CancellationInfo
                        {
                            CancelNumber = string.Empty,
                            CancelDate = string.Empty,
                            CancelReason = string.Empty,
                            Status = string.Empty
                        },


                        ReservationComments = bi?.BookingComments?
                            .Select(c => new ReservationComment
                            {
                                Category = c.Attribute,
                                Comment = c.Value
                            })
                            .ToList(),

                        Comment = string.Empty,
                        Category = string.Empty,


                        Properties =
                        [
                            new PropertyDetails
                            {
                                BasicPropertyInfoSummary = new BasicPropertyInfoSummary
                                {
                                    PropertyCode = bi.ParentCode,
                                    PropertyCategory = bi?.ItemAttributes?
                                    .FirstOrDefault(a => string.Equals(a.Attribute, "propertyCategory", StringComparison.OrdinalIgnoreCase))
                                    ?.Value ?? string.Empty,
                                    PropertySegment = bi?.ItemAttributes?
                                    .FirstOrDefault(a => string.Equals(a.Attribute, "segment", StringComparison.OrdinalIgnoreCase))
                                    ?.Value ?? string.Empty,
                                    PmsID = string.Empty,
                                    GuaranteeRequired = false,
                                    Policies = new Policies
                                    {
                                        CancellationPolicy = new CancellationPolicy
                                        {
                                            PolicyCode = string.Empty,
                                            Description = string.Empty,
                                            Deadline = string.Empty,
                                        },
                                    },
                                    UnitStays =
                                    [
                                        new GetUnitStay
                                        {
                                            UnitTypeCode = bi?.ItemCode,
                                            UnitDescription = bi?.Description,
                                            RatePlan =
                                            [
                                                new GetRatePlan
                                                {
                                                    RatePlanCode = bi?.ItemAttributes?
                                                        .FirstOrDefault(a => string.Equals(a.Attribute, "ratePlan", StringComparison.OrdinalIgnoreCase))
                                                        ?.Value ?? string.Empty,
                                                    GuaranteeRequired = false,
                                                    EffectiveDate = string.Empty
                                                },
                                            ],
                                            SpecialRequests = bi?.ItemAttributes?
                                                .Where(a => string.Equals(a.Attribute, "specialRequest", StringComparison.OrdinalIgnoreCase))
                                                .Select(a => new GetSpecialRequest
                                                {
                                                    RequestCode = string.Empty,
                                                    Text = a.Value
                                                })
                                                .ToList() ?? [],

                                            RoomRates =
                                            [
                                                new GetRoomRate
                                                {
                                                    NumberOfUnits = 1,
                                                    Rates =
                                                    [
                                                        new GetRateDetail
                                                        {
                                                            Base = new GetBase
                                                            {
                                                                AmountBeforeTax = bi?.PriceLevel?.TotalPrice != null ? (double)bi.PriceLevel.TotalPrice : 0,
                                                                CurrencyCode = bi?.PriceLevel.Currency,
                                                                DecimalPlaces = 2,
                                                            },
                                                            Taxes = bi?.PriceLevel.PriceAttributes?
                                                                .Where(p => string.Equals(p.Attribute, "taxesTotal", StringComparison.OrdinalIgnoreCase))
                                                                .Select(p => new GetTax
                                                                {
                                                                    Amount = double.TryParse(p.Value, out var t) ? t : 0,
                                                                    CurrencyCode = bi?.PriceLevel.Currency,
                                                                    DecimalPlaces = 2,
                                                                    Type = "Tax",
                                                                    Description = "Tax"
                                                                }).ToList() ?? [],
                                                            Fees = bi?.PriceLevel.PriceAttributes?
                                                                .Where(p => string.Equals(p.Attribute, "feesTotal", StringComparison.OrdinalIgnoreCase))
                                                                .Select(p => new GetFee
                                                                {
                                                                    Amount = double.TryParse(p.Value, out var f) ? f : 0,
                                                                    CurrencyCode = bi?.PriceLevel.Currency,
                                                                    DecimalPlaces = 2,
                                                                    Type = "Fee",
                                                                    Description = "Fee"
                                                                }).ToList() ?? [],
                                                            Total = new GetTotal
                                                            {
                                                                AmountAfterTax = bi?.PriceLevel.TotalPrice != null ? (double)bi.PriceLevel.TotalPrice : 0,
                                                                CurrencyCode = bi?.PriceLevel.Currency,
                                                                DecimalPlaces = 2
                                                            },
                                                        },

                                                    ],

                                                },

                                            ],
                                            GuestCounts =
                                            [
                                                new GetGuestCount
                                                {
                                                    AgeQualifyingCode = "10", //Adults
                                                    Count = bi?.GuestCounts?.Adults ?? 0,
                                                },
                                                new GetGuestCount
                                                {
                                                    AgeQualifyingCode = "8", //Children
                                                    Count = bi?.GuestCounts?.Children ?? 0,
                                                }
                                            ],
                                            UnitStayDates = new GetUnitStayDates
                                            {
                                                Start = bi?.StartDate.ToString("yyyy-MM-dd"),
                                                End = bi?.EndDate.ToString("yyyy-MM-dd")
                                            },

                                            Guarantee = new GetGuarantee
                                            {
                                                GuaranteeType = bi?.BookingAttributes?
                                                    .FirstOrDefault(a => string.Equals(a.Attribute, "guaranteeType", StringComparison.OrdinalIgnoreCase))
                                                    ?.Value ?? string.Empty,

                                                Deadline = bi?.BookingAttributes?
                                                    .FirstOrDefault(a => string.Equals(a.Attribute, "guaranteeDeadline", StringComparison.OrdinalIgnoreCase))
                                                    ?.Value ?? string.Empty,

                                                Amount = 0,
                                                CurrencyCode = "USD",
                                                GuaranteedAccepted = false
                                            },
                                        }
                                    ],
                                    LoyaltyRedemption = new Model.Reservation.Shared.LoyaltyRedemption
                                    {
                                        CertificateNumber = bi?.BookingAttributes?
                                                    .FirstOrDefault(a => string.Equals(a.Attribute, "certificateNumber", StringComparison.OrdinalIgnoreCase))
                                                    ?.Value ?? string.Empty,
                                        ProgramName = bi?.BookingAttributes?
                                                    .FirstOrDefault(a => string.Equals(a.Attribute, "programName", StringComparison.OrdinalIgnoreCase))
                                                    ?.Value ?? string.Empty,
                                        MembershipId = bi?.BookingAttributes?
                                                    .FirstOrDefault(a => string.Equals(a.Attribute, "MembershipId", StringComparison.OrdinalIgnoreCase))
                                                    ?.Value ?? string.Empty,
                                        PromotionCode = bi?.BookingAttributes?
                                                    .FirstOrDefault(a => string.Equals(a.Attribute, "PromotionCode", StringComparison.OrdinalIgnoreCase))
                                                    ?.Value ?? string.Empty,
                                        RedemptionQuantity = double.TryParse(bi?.BookingAttributes?
                                        .FirstOrDefault(a => a.Attribute.Equals("redemptionQuantity", StringComparison.OrdinalIgnoreCase))
                                        ?.Value, out var result) ? result : 0,
                                        CurrencyCode = bi?.BookingAttributes?
                                                    .FirstOrDefault(a => string.Equals(a.Attribute, "currencyCode", StringComparison.OrdinalIgnoreCase))
                                                    ?.Value ?? string.Empty,
                                    },
                                    GuestProfiles = null,
                                    Profiles =
                                    [
                                        new Profile
                                        {
                                            MemberProfile = new MemberProfile
                                            {
                                                FirstName = bi?.GuestInfo?.FirstName ?? string.Empty,
                                                LastName = bi?.GuestInfo?.LastName ?? string.Empty,
                                                ContactInfo = new ContactInfo
                                                {
                                                    Addresses = bi?.GuestInfo?.PostalAddresses?
                                                            .Select(pa => new Address
                                                            {
                                                                AddressLine1 = pa.Line1 ?? string.Empty,
                                                                AddressLine2 = pa.Line2 ?? string.Empty,
                                                                AddressType = pa.AddressType ?? string.Empty,
                                                                CityName = pa.City ?? string.Empty,
                                                                Country = pa.Country ?? string.Empty,
                                                                CountryCode = string.Empty,
                                                                IsPrimary = pa.Sequence == 1,
                                                                Latitude = 0,
                                                                Longitude = 0,
                                                                PostalCode = pa.PostalCode ?? string.Empty,
                                                                State = pa.State ?? string.Empty,
                                                                StateProv = string.Empty,
                                                            })
                                                            .ToList() ?? []
                                                },

                                                DateOfBirth = string.Empty,
                                                Sex = string.Empty,
                                                SourceSystemIds =
                                                [
                                                    new SourceSystemId
                                                    {
                                                        SourceSystem = string.Empty,
                                                        Id = string.Empty,
                                                    },
                                                ],
                                                IsOccupied = false,
                                                ProfileType = string.Empty,
                                            },
                                        },
                                    ],
                                },
                            },

                        ]
                    };

                    list.Add(reservation);
                }
            }

            return list;
        }
        private static List<ReservationDetails> MapSearchReservations(OnexSearchReservationResponse source)
        {
            var list = new List<ReservationDetails>();

            if (source.Data?.Results != null && source.Data.Results.Count != 0)
            {
                foreach (var bi in source.Data.Results)
                {
                    var reservation = new ReservationDetails
                    {
                        // reservation-level fields
                        Status = bi?.Action,
                        ReservationDate = bi?.ReservationDate?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        ArrivalDate = bi?.StartDate,
                        DepartureDate = bi?.EndDate,

                        SourceReservationIds =
                        [
                            new SourceReservationId
                            {
                                SourceSystem = source.Data?.ExternalReference?.SourceSystem ?? string.Empty,
                                Id = source.Data?.ExternalReference?.SourceReferenceId ?? string.Empty
                            },
                            new SourceReservationId
                            {
                                SourceSystem = "EXCHANGE",
                                Id = bi?.MemberID ?? string.Empty
                            }
                        ],


                        CancellationInfo = new CancellationInfo
                        {
                            CancelNumber = string.Empty,
                            CancelDate = string.Empty,
                            CancelReason = string.Empty,
                            Status = string.Empty
                        },


                        ReservationComments = bi?.BookingComments?
                            .Select(c => new ReservationComment
                            {
                                Category = c.Attribute,
                                Comment = c.Value
                            })
                            .ToList(),

                        Comment = string.Empty,
                        Category = string.Empty,


                        Properties =
                        [
                            new PropertyDetails
                            {
                                BasicPropertyInfoSummary = new BasicPropertyInfoSummary
                                {
                                    PropertyCode = bi.ParentCode,
                                    PropertyCategory = bi?.ItemAttributes?
                                    .FirstOrDefault(a => string.Equals(a.Attribute, "propertyCategory", StringComparison.OrdinalIgnoreCase))
                                    ?.Value ?? string.Empty,
                                    PropertySegment = bi?.ItemAttributes?
                                    .FirstOrDefault(a => string.Equals(a.Attribute, "segment", StringComparison.OrdinalIgnoreCase))
                                    ?.Value ?? string.Empty,
                                    PmsID = string.Empty,
                                    GuaranteeRequired = false,
                                    Policies = new Policies
                                    {
                                        CancellationPolicy = new CancellationPolicy
                                        {
                                            PolicyCode = string.Empty,
                                            Description = string.Empty,
                                            Deadline = string.Empty,
                                        },
                                    },
                                    UnitStays =
                                    [
                                        new GetUnitStay
                                        {
                                            UnitTypeCode = bi?.ItemCode,
                                            UnitDescription = bi?.Description,
                                            RatePlan =
                                            [
                                                new GetRatePlan
                                                {
                                                    RatePlanCode = bi?.ItemAttributes?
                                                        .FirstOrDefault(a => string.Equals(a.Attribute, "ratePlan", StringComparison.OrdinalIgnoreCase))
                                                        ?.Value ?? string.Empty,
                                                    GuaranteeRequired = false,
                                                    EffectiveDate = string.Empty
                                                },
                                            ],
                                            SpecialRequests = bi?.ItemAttributes?
                                                .Where(a => string.Equals(a.Attribute, "specialRequest", StringComparison.OrdinalIgnoreCase))
                                                .Select(a => new GetSpecialRequest
                                                {
                                                    RequestCode = string.Empty,
                                                    Text = a.Value
                                                })
                                                .ToList() ?? [],

                                            RoomRates =
                                            [
                                                new GetRoomRate
                                                {
                                                    NumberOfUnits = 1,
                                                    Rates =
                                                    [
                                                        new GetRateDetail
                                                        {
                                                            Base = new GetBase
                                                            {
                                                                AmountBeforeTax = bi?.PriceLevel?.TotalPrice != null ? (double)bi.PriceLevel.TotalPrice : 0,
                                                                CurrencyCode = bi?.PriceLevel.Currency,
                                                                DecimalPlaces = 2,
                                                            },
                                                            Taxes = bi?.PriceLevel.PriceAttributes?
                                                                .Where(p => string.Equals(p.Attribute, "taxesTotal", StringComparison.OrdinalIgnoreCase))
                                                                .Select(p => new GetTax
                                                                {
                                                                    Amount = double.TryParse(p.Value, out var t) ? t : 0,
                                                                    CurrencyCode = bi?.PriceLevel.Currency,
                                                                    DecimalPlaces = 2,
                                                                    Type = "Tax",
                                                                    Description = "Tax"
                                                                }).ToList() ?? [],
                                                            Fees = bi?.PriceLevel.PriceAttributes?
                                                                .Where(p => string.Equals(p.Attribute, "feesTotal", StringComparison.OrdinalIgnoreCase))
                                                                .Select(p => new GetFee
                                                                {
                                                                    Amount = double.TryParse(p.Value, out var f) ? f : 0,
                                                                    CurrencyCode = bi?.PriceLevel.Currency,
                                                                    DecimalPlaces = 2,
                                                                    Type = "Fee",
                                                                    Description = "Fee"
                                                                }).ToList() ?? [],
                                                            Total = new GetTotal
                                                            {
                                                                AmountAfterTax = bi?.PriceLevel.TotalPrice != null ? (double)bi.PriceLevel.TotalPrice : 0,
                                                                CurrencyCode = bi?.PriceLevel.Currency,
                                                                DecimalPlaces = 2
                                                            },
                                                        },
                                                    ],

                                                },

                                            ],
                                            GuestCounts =
                                            [
                                                new GetGuestCount
                                                {
                                                    AgeQualifyingCode = "10", //Adults
                                                    Count = bi?.GuestCounts?.Adults ?? 0,
                                                },
                                                new GetGuestCount
                                                {
                                                    AgeQualifyingCode = "8", //Children
                                                    Count = bi?.GuestCounts?.Children ?? 0,
                                                }
                                            ],
                                            UnitStayDates = new GetUnitStayDates
                                            {
                                                Start = bi?.StartDate.ToString(),
                                                End = bi?.EndDate.ToString()
                                            },

                                            Guarantee = new GetGuarantee
                                            {
                                                GuaranteeType = bi?.BookingAttributes?
                                                    .FirstOrDefault(a => string.Equals(a.Attribute, "guaranteeType", StringComparison.OrdinalIgnoreCase))
                                                    ?.Value ?? string.Empty,

                                                Deadline = bi?.BookingAttributes?
                                                    .FirstOrDefault(a => string.Equals(a.Attribute, "guaranteeDeadline", StringComparison.OrdinalIgnoreCase))
                                                    ?.Value ?? string.Empty,

                                                Amount = 0,
                                                CurrencyCode = "USD",
                                                GuaranteedAccepted = false
                                            },
                                        }
                                    ],
                                    LoyaltyRedemption = new Model.Reservation.Shared.LoyaltyRedemption
                                    {
                                        CertificateNumber = bi?.BookingAttributes?
                                        .FirstOrDefault(a => string.Equals(a.Attribute, "certificateNumber", StringComparison.OrdinalIgnoreCase))
                                        ?.Value ?? string.Empty,
                                        ProgramName = bi?.BookingAttributes?
                                        .FirstOrDefault(a => string.Equals(a.Attribute, "programName", StringComparison.OrdinalIgnoreCase))
                                        ?.Value ?? string.Empty,
                                        MembershipId = bi?.BookingAttributes?
                                        .FirstOrDefault(a => string.Equals(a.Attribute, "MembershipId", StringComparison.OrdinalIgnoreCase))
                                        ?.Value ?? string.Empty,
                                        PromotionCode = bi?.BookingAttributes?
                                        .FirstOrDefault(a => string.Equals(a.Attribute, "PromotionCode", StringComparison.OrdinalIgnoreCase))
                                        ?.Value ?? string.Empty,
                                        RedemptionQuantity = double.TryParse(bi?.BookingAttributes?
                                        .FirstOrDefault(a => a.Attribute.Equals("redemptionQuantity", StringComparison.OrdinalIgnoreCase))
                                        ?.Value, out var result) ? result : 0,
                                        CurrencyCode = bi?.BookingAttributes?
                                        .FirstOrDefault(a => string.Equals(a.Attribute, "currencyCode", StringComparison.OrdinalIgnoreCase))
                                        ?.Value ?? string.Empty,
                                    },
                                    GuestProfiles = null,
                                    Profiles =
                                    [
                                        new Profile
                                        {
                                            MemberProfile = new MemberProfile
                                            {
                                                FirstName = bi?.GuestInfo?.FirstName ?? string.Empty,
                                                LastName = bi?.GuestInfo?.LastName ?? string.Empty,
                                                ContactInfo = new ContactInfo
                                                {
                                                    Addresses = bi?.GuestInfo?.PostalAddresses?
                                                    .Select(pa => new Address
                                                    {
                                                        AddressLine1 = pa.Line1 ?? string.Empty,
                                                        AddressLine2 = pa.Line2 ?? string.Empty,
                                                        AddressType = pa.AddressType ?? string.Empty,
                                                        CityName = pa.City ?? string.Empty,
                                                        Country = pa.Country ?? string.Empty,
                                                        CountryCode = string.Empty,
                                                        IsPrimary = pa.Sequence == 1,
                                                        Latitude = 0,
                                                        Longitude = 0,
                                                        PostalCode = pa.PostalCode ?? string.Empty,
                                                        State = pa.State ?? string.Empty,
                                                        StateProv = string.Empty,
                                                    })
                                                    .ToList() ?? []
                                                },
                                                DateOfBirth = string.Empty,
                                                Sex = string.Empty,
                                                SourceSystemIds =
                                                [
                                                    new SourceSystemId
                                                    {
                                                        SourceSystem = string.Empty,
                                                        Id = string.Empty,
                                                    },
                                                ],
                                                IsOccupied = false,
                                                ProfileType = string.Empty,
                                            },
                                        },
                                    ],
                                },
                            },

                        ],

                    };

                    list.Add(reservation);
                }
            }

            return list;
        }


        private static string MapAddressType(string otaType)
        {
            if (string.IsNullOrWhiteSpace(otaType))
            {
                return "other";
            }

            return otaType.Trim().ToUpperInvariant() switch
            {
                "HOME" or "MAILING" or "VACATION" or "TEMPORARY" => "residence",
                "BUSINESS" or "BILLING" or "SHIPPING" => "business",
                _ => "other",
            };
        }

        private static List<BookingItemRequest> MapOnexBookingItems(OTACreateReservationRequest ota)
        {
            var bookingItems = new List<BookingItemRequest>();
            var property = ota?.ReservationCandidates?.Properties?.BasicPropertyInfoSummary;
            if (property?.UnitStays == null)
            {
                return bookingItems;
            }

            var guestProfile = ota?.ReservationCandidates?.Profiles?.FirstOrDefault()?.MemberProfile;
            var roomRate = ota.ReservationCandidates?.Properties?.BasicPropertyInfoSummary?.UnitStays?.FirstOrDefault()?.RoomRates?.FirstOrDefault();

            var usdRate = roomRate?.Rates?.FirstOrDefault(r => r?.Total?.CurrencyCode?.Equals("USD", StringComparison.OrdinalIgnoreCase) == true)
                        ?? roomRate?.Rates?.FirstOrDefault();
            var lr = ota.ReservationCandidates?.LoyaltyRedemption;
            foreach (var unit in property.UnitStays ?? [])
            {
                var allAddresses = guestProfile.ContactInfo?.Addresses ?? [];

                var orderedAddresses = allAddresses
                    .OrderByDescending(a => a.IsPrimary) // Primary first
                    .ToList();

                var postalAddresses = orderedAddresses
                    .Select((addr, idx) => new PostalAddress
                    {
                        Sequence = idx + 1,
                        AddressType = MapAddressType(addr.AddressType),
                        Line1 = addr?.AddressLine1,
                        Line2 = addr?.AddressLine2,
                        City = addr?.CityName,
                        State = addr?.StateProv ?? addr.State,
                        PostalCode = addr?.PostalCode,
                        Country = addr?.Country
                    })
                    .ToList();
                var item = new BookingItemRequest
                {
                    ItemCode = unit.UnitTypeCode,
                    ItemClass = "UnitType",
                    ParentCode = property.PropertyCode,
                    Name = unit.UnitDescription,
                    Description = unit.UnitDescription,
                    ItemAttributes =
                    [
                        new AttributePair
                        {
                            Attribute = "propertyCategory",
                            Value = property.PropertyCategory ?? string.Empty
                        },
                        new AttributePair
                        {
                            Attribute = "segment",
                            Value = property.PropertySegment ?? string.Empty
                        }
                    ],
                    MemberID = guestProfile?.SourceSystemIds?
                                .FirstOrDefault(x =>
                                    !string.IsNullOrEmpty(x.SourceSystem) &&
                                    x.SourceSystem.Equals("EXCHANGE", StringComparison.OrdinalIgnoreCase))
                                ?.Id ?? "0",
                    StartDate = DateTimeOffset.TryParse(unit.UnitStayDates.Start, CultureInfo.InvariantCulture, out var startDate) ? startDate : DateTimeOffset.MinValue,
                    EndDate = DateTimeOffset.TryParse(unit.UnitStayDates.End, CultureInfo.InvariantCulture, out var endDate) ? endDate : DateTimeOffset.MinValue,
                    WeekNumber = 1,
                    Action = "Initiate",
                    IsOwnerBooking = true,
                    GuestCounts = new GuestCounts
                    {
                        Adults = Math.Max(1, unit.GuestCounts?.Where(g => g.AgeQualifyingCode == 10).Sum(g => g.Count) ?? 0),
                        Children = unit.GuestCounts?.Where(g => g.AgeQualifyingCode <= 8).Sum(g => g.Count) ?? 0
                    },
                    GuestInfo = new GuestInfo
                    {
                        FirstName = guestProfile.FirstName,
                        LastName = guestProfile.LastName,

                        // Map all addresses
                        PostalAddresses = postalAddresses,
                        DigitalAddresses = [],
                        PhoneNumbers = []
                    },
                    PriceLevel = new PriceLevel
                    {
                        Status = true, // optional
                        PriceLevelCode = unit.RatePlan?.FirstOrDefault()?.RatePlanCode ?? string.Empty,
                        Currency = usdRate?.Total?.CurrencyCode ?? string.Empty,
                        TotalPrice = (decimal)(usdRate?.Total?.AmountAfterTax ?? 0),
                        PriceAttributes =
                        [
                            new AttributePair
                            {
                                Attribute = "amountBeforeTax",
                                Value = usdRate?.Base?.AmountBeforeTax?.ToString() ?? "0"
                            },// Map total taxes
                            new AttributePair
                            {
                                Attribute = "taxesTotal",
                                Value = usdRate?.Taxes?.Sum(t => t.Amount).ToString() ?? "0"
                            },// Map total fees
                            new AttributePair
                            {
                                Attribute = "feesTotal",
                                Value = usdRate?.Fees?.Sum(f => f.Amount).ToString() ?? "0"
                            }
                        ]
                    },

                    BookingAttributes = [.. (unit.Guarantee != null
                            ? new List<AttributePair>
                            {
                                new() {
                                    Attribute = "guaranteeType",
                                    Value = unit.Guarantee.GuaranteeType
                                },
                                new() {
                                    Attribute = "guaranteeDeadline",
                                    Value = unit.Guarantee.Deadline
                                }
                            }
                            : []
                        )
                        .Concat(
                        // loyalty redemption
                            lr != null
                                ? new Dictionary<string, string>
                                {
                                    { nameof(lr.CertificateNumber), lr.CertificateNumber ?? string.Empty },
                                    { nameof(lr.ProgramName), lr.ProgramName ?? string.Empty },
                                    { nameof(lr.MembershipId), lr.MembershipId ?? string.Empty },
                                    { nameof(lr.PromotionCode), lr.PromotionCode ?? string.Empty },
                                    { nameof(lr.RedemptionQuantity), lr.RedemptionQuantity.ToString() ?? "25"},
                                    { nameof(lr.CurrencyCode), lr.CurrencyCode ?? "USD" },
                                }
                                .Select(x => new AttributePair
                                {
                                    Attribute = x.Key,
                                    Value = x.Value
                                })
                                .ToList()
                                : []
                        )],

                    // BookingComments (merge reservation comments + special requests + optional payments)
                    BookingComments = [..
                        (ota.ReservationCandidates?.ReservationComments ?? [])
                        .Select(c => new AttributePair
                        {
                            Attribute = c.Category?.ToUpper() ?? "GENERAL",
                            Value = c.Comment ?? string.Empty
                        })
                        .Concat(
                            unit.SpecialRequests.Select(sr => new AttributePair
                            {
                                Attribute = "SPECIALREQUEST",
                                Value = sr.Text ?? string.Empty
                            })
                        )
                    ],
                };

                // Add all SpecialRequests from unit as ItemAttributes
                if (unit.SpecialRequests != null && unit.SpecialRequests.Count > 0)
                {
                    item.ItemAttributes.AddRange(
                        unit.SpecialRequests.Select(sr => new AttributePair
                        {
                            Attribute = sr.RequestCode ?? string.Empty,
                            Value = sr.Text ?? string.Empty
                        })
                    );
                }

                bookingItems.Add(item);
            }
            return bookingItems;

        }
    }
}
