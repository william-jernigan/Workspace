using APITransformationsFunction.Common;
using APITransformationsFunction.Model.Search;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace APITransformationsFunction.Mappers
{
    public static class InventorySearchMapper
    {
        public static ONEXInventorySearchRequest MapOtaToOnexRequest(OTAInventorySearchRequest otaRequest)
        {
            if (otaRequest == null)
            {
                return null;
            }

            var request = new ONEXInventorySearchRequest
            {
                Id = otaRequest.CorrelationID,
                Source = "inventory-service",
                Specversion = "1.0",
                Type = "inventory.search.request",
                CorrelationId = otaRequest.CorrelationID,
                Datacontenttype = "application/json",
                Dataschema = new Uri("http://example.com/schemas/inventory-search"),
                Subject = "Inventory Search Request",
                Time = DateTime.UtcNow,
                Ispartial = false,
                Data = new ONEXInventorySearchData
                {
                    SystemMeta = new ONEXSystemMeta
                    {
                        TenantID = "HGV",
                        InsertedBy = "search-service",
                        InsertedDateTime = DateTime.UtcNow,
                        UpdatedBy = "search-service",
                        UpdatedDateTime = DateTime.UtcNow,
                        Status = true,
                        StatusReason = "Active"
                    },
                    SearchCriteria = new ONEXSearchCriteria
                    {
                        TradingAgreementCode = otaRequest.TradingAgreementCode,
                        PropertiesOnly = otaRequest.PropertySearchCriteria.PropertiesOnly,
                        PropertyCodes = otaRequest.PropertySearchCriteria.PropertyCodes,
                        UnitTypeCode = string.Empty,
                        GeoLocation = otaRequest.PropertySearchCriteria.GeoLocation == null ? null : new ONEXGeoLocation
                        {
                            Latitude = otaRequest.PropertySearchCriteria.GeoLocation.Latitude,
                            Longitude = otaRequest.PropertySearchCriteria.GeoLocation.Longitude,
                            RadiusDistance = otaRequest.PropertySearchCriteria.GeoLocation.RadiusDistance,
                            DistanceUnit = otaRequest.PropertySearchCriteria.GeoLocation.DistanceUnit
                        },
                        StayDateRange = otaRequest.PropertySearchCriteria.StayDateRange == null ? null : new ONEXStayDateRange
                        {
                            Start = otaRequest.PropertySearchCriteria.StayDateRange.Start,
                            End = otaRequest.PropertySearchCriteria.StayDateRange.End,
                            FlexibleDays = otaRequest.PropertySearchCriteria.StayDateRange.FlexibleDays
                        },
                        RateRange = otaRequest.PropertySearchCriteria.RateRange == null ? null : new ONEXRateRange
                        {
                            MinRate = (double)otaRequest.PropertySearchCriteria.RateRange.MinRate,
                            MaxRate = (double)otaRequest.PropertySearchCriteria.RateRange.MaxRate,
                            CurrencyCode = otaRequest.PropertySearchCriteria.RateRange.CurrencyCode,
                            RateTimeUnit = otaRequest.PropertySearchCriteria.RateRange.RateTimeUnit
                        },
                        RatePlans = otaRequest.PropertySearchCriteria.RatePlans?.Select(r => new ONEXRatePlan
                        {
                            RatePlanCode = r.RatePlanCode
                        }).ToList() ?? [],
                        PropertyAmenities = otaRequest.PropertySearchCriteria.PropertyAmenities?.Select(a => new ONEXPropertyAmenity
                        {
                            Code = a.Code
                        }).ToList() ?? [],
                        GuestCounts = otaRequest.PropertySearchCriteria.GuestCounts?.Select(g => new ONEXGuestCount
                        {
                            Count = g.Count,
                            AgeQualifyingCode = g.AgeQualifyingCode
                        }).ToList() ?? [],
                        LoyaltyPrograms = otaRequest.PropertySearchCriteria.LoyaltyPrograms?.Select(l => new ONEXLoyaltyProgram
                        {
                            ProgramCode = l.ProgramCode
                        }).ToList() ?? [],
                        SortBy = otaRequest.PropertySearchCriteria.OrderBy == null ? null : new ONEXOrderBy
                        {
                            Field = otaRequest.PropertySearchCriteria.OrderBy.Field,
                            Direction = otaRequest.PropertySearchCriteria.OrderBy.Direction
                        },
                        Include = new ONEXIncludeFlags
                        {
                            PropertyMetadata = otaRequest.IncludePropertyMetadata,
                            RateMetadata = otaRequest.IncludeRateMetadata,
                            ContactInfo = otaRequest.IncludeContactInfo,
                            UnitMetadata = otaRequest.IncludeUnitMetadata,
                            AvailablePropertiesOnly = otaRequest.IncludeAvailablePropertiesOnly
                        }
                    }
                }
            };
            return request;
        }
        public static OTAInventorySearchResponse MapOnexToOtaResponse(ONEXInventorySearchResponse onexResponse, OTAInventorySearchRequest otaRequest)
        {
            if (onexResponse == null || onexResponse.Data?.InventoryItems == null)
            {
                return null;
            }

            var otaResponse = new OTAInventorySearchResponse
            {
                CorrelationID = onexResponse.CorrelationId,
                TradingAgreementCode = otaRequest.TradingAgreementCode,
                Pagination = new Pagination
                {
                    Page = onexResponse.Data.InventoryItems.CurrentPage,
                    PageSize = onexResponse.Data.InventoryItems.PageSize,
                    TotalItems = onexResponse.Data.InventoryItems.TotalItems,
                    TotalPages = onexResponse.Data.InventoryItems.TotalPages
                },
                Properties = [],
                RatePlans = []
            };

            // Group items by parentCode -> each group is a property
            var groupedProperties = onexResponse.Data.InventoryItems.Items
                .Where(i => !string.IsNullOrWhiteSpace(i.ParentCode))
                .GroupBy(i => i.ParentCode);

            foreach (var groupedProperty in groupedProperties)
            {
                var firstItem = groupedProperty.FirstOrDefault();
                if (firstItem == null)
                {
                    continue;
                }

                var property = new PropertyItem
                {
                    BasicPropertyInfoSummary = new BasicPropertyInfoSummary
                    {
                        PropertyCode = firstItem.ParentCode,
                        Name = firstItem.ParentCode,
                        Status = "Active",
                        ChainCode = ApiConstants.CLAPartnerCode,
                        BrandCode = ApiConstants.CLAPartnerCode
                    },
                    UnitTypes = []
                };

                foreach (var item in groupedProperty)
                {
                    var availability = item.ItemAvailability;

                    var unitType = new UnitType
                    {
                        NumberOfUnits = groupedProperty.Count(),
                        UnitTypeCode = item.ItemCode,
                        Availability = new Availability
                        {
                            CheckIn = otaRequest.PropertySearchCriteria.StayDateRange.Start,
                            CheckOut = otaRequest.PropertySearchCriteria.StayDateRange.End,
                            Nights = (int)(otaRequest.PropertySearchCriteria.StayDateRange.End - otaRequest.PropertySearchCriteria.StayDateRange.Start).TotalDays,
                            Quantity = availability?.QuantityOnHand ?? 0,
                            RoomRates = []
                        }
                    };
                    // Conditionally add metadata
                    if (otaRequest.IncludeUnitMetadata)
                    {
                        unitType.SmokingInd = item.ItemAttributes.Any(a => a.Attribute.Equals("SmokingRoom", StringComparison.OrdinalIgnoreCase) &&
                            a.Value.Equals("true", StringComparison.OrdinalIgnoreCase));
                        unitType.CompositeInd = false;
                        unitType.Description = new UnitDescription
                        {
                            Name = item.Name,
                            Text = item.Description
                        };
                        unitType.UnitSize = new UnitSize
                        {
                            Size = 0,
                            UnitOfMeasure = availability?.UnitOfMeasure ?? string.Empty
                        };
                        unitType.UnitOccupancy = new UnitOccupancy
                        {
                            MinOccupancy = 1,
                            MaxOccupancy = item.MaximumOccupancy,
                            StandardOccupancy = item.MaximumOccupancy
                        };
                        unitType.BedType = new BedType();
                        // BedType Mapping
                        var bedTypeAttr = item.ItemAttributes.FirstOrDefault(x =>
                            x.Attribute.Equals("BedType", StringComparison.OrdinalIgnoreCase));
                        if (bedTypeAttr != null)
                        {
                            unitType.BedType.Code = bedTypeAttr.Attribute;
                            unitType.BedType.Description = bedTypeAttr.Value;
                        }
                    }

                    // Price Levels Mapping
                    if (availability?.ItemSchedulePriceLevels != null)
                    {
                        foreach (var itemSchedulePriceLevel in availability?.ItemSchedulePriceLevels)
                        {
                            var attrs = itemSchedulePriceLevel.SchedulePriceAttributes ?? [];
                            string GetAttributeValue(string k) => attrs.FirstOrDefault(x => x.Attribute.Equals(k, StringComparison.OrdinalIgnoreCase))?.Value ?? "0";
                            var roomRate = new RoomRate
                            {
                                RatePlanCode = GetAttributeValue("RatePlanCode"),
                                Rates =
                                [
                                    new Rate
                                    {
                                        Base = new BaseRate
                                        {
                                            AmountBeforeTax = GetAttributeValue("AmountBeforeTax"),
                                            CurrencyCode = itemSchedulePriceLevel.Currency
                                        },
                                        Taxes =
                                        [
                                            new Tax
                                            {
                                                Amount = decimal.Parse(GetAttributeValue("TotalTaxes")),
                                                CurrencyCode = itemSchedulePriceLevel.Currency
                                            }
                                        ],
                                        Fees = [
                                            new Fee
                                            {
                                                Amount = decimal.Parse(GetAttributeValue("TotalServiceCharges")),
                                                Description = GetAttributeValue("ServiceChargeDesc"),
                                                CurrencyCode = itemSchedulePriceLevel.Currency
                                            }
                                        ],
                                        Total = new TotalRate
                                        {
                                            AmountAfterTax = GetAttributeValue("AmountAfterTax"),
                                            CurrencyCode = itemSchedulePriceLevel.Currency,
                                            DecimalPlaces = 2
                                        }
                                    }
                                ],
                                NightlyRates = []
                            };
                            unitType.Availability.RoomRates.Add(roomRate);
                        }
                    }
                    property.UnitTypes.Add(unitType);

                    if (otaRequest.IncludeRateMetadata)
                    {
                        // RatePlan Mapping
                        otaResponse.RatePlans = [];
                        var ratePlan = new RatePlan
                        {
                            RatePlanCode = item.ItemAttributes.FirstOrDefault(x => x.Attribute.Equals("RatePlanCode", StringComparison.OrdinalIgnoreCase))?.Value,
                            IdRequiredInd = bool.TryParse(item.ItemAttributes.FirstOrDefault(x => x.Attribute.Equals("ClientIdRequired", StringComparison.OrdinalIgnoreCase))?.Value, out var idRequiredInd) && idRequiredInd,
                            Guarantee = new Guarantee
                            {
                                GuaranteeType = item.ItemAttributes.FirstOrDefault(x => x.Attribute.Equals("GuarPolicyCode", StringComparison.OrdinalIgnoreCase))?.Value,
                                GuaranteesAccepted = [.. (item.ItemAttributes.FirstOrDefault(x => x.Attribute.Equals("GuaranteeMethods", StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries).Select(code => new GuaranteeAccepted { GuaranteeTypeCode = code.Trim() })],
                                GuaranteeDescription = new GuaranteeDescription
                                {
                                    Text = item.ItemAttributes.FirstOrDefault(x => x.Attribute.Equals("GuarPolicyDesc", StringComparison.OrdinalIgnoreCase))?.Value
                                }
                            },

                            CancelPenalties =
                            [
                                new CancelPenalty
                                {
                                    NonRefundable = false,
                                    Deadline = new Deadline
                                    {
                                        AbsoluteDeadline = DateTime.TryParse(
                                            item.ItemAttributes
                                                .FirstOrDefault(x => x.Attribute.Equals("CxlPolicyDeadline", StringComparison.OrdinalIgnoreCase))
                                                ?.Value, out var deadline) ? deadline : DateTime.MinValue
                                    },
                                    PenaltyDescription = [.. item.ItemAttributes
                                        .Where(x => x.Attribute.Equals("CxlPolicyDesc", StringComparison.OrdinalIgnoreCase))
                                        .Select(x => new PenaltyDescription { Text = x?.Value })]
                                }
                            ],
                            RatePlanDescription = new RatePlanDescription
                            {
                                Name = item.ItemAttributes.FirstOrDefault(x => x.Attribute.Equals("RatePlanCode", StringComparison.OrdinalIgnoreCase))?.Value,
                                Text = item.ItemAttributes.FirstOrDefault(x => x.Attribute.Equals("RatePlanDesc", StringComparison.OrdinalIgnoreCase))?.Value
                            }
                        };
                        otaResponse.RatePlans.Add(ratePlan);
                    }
                }
                otaResponse.Properties.Add(property);
            }
            return otaResponse;
        }
        public static OTAErrorResponse MapBackendError(HttpResponseMessage backendResponse)
        {
            var statusCode = backendResponse.StatusCode;
            var correlationId = Guid.NewGuid();

            return new OTAErrorResponse
            {
                Type = statusCode switch
                {
                    HttpStatusCode.BadRequest => "https://api.hgv.com/errors/bad-request",
                    HttpStatusCode.Unauthorized => "https://api.hgv.com/errors/unauthorized",
                    HttpStatusCode.Forbidden => "https://api.hgv.com/errors/forbidden",
                    HttpStatusCode.NotFound => "https://api.hgv.com/errors/not-found",
                    HttpStatusCode.InternalServerError => "https://api.hgv.com/errors/internal-server-error",
                    HttpStatusCode.PaymentRequired => "https://api.hgv.com/errors/payment-required",
                    HttpStatusCode.NotImplemented => "https://api.hgv.com/errors/not-implemented",
                    HttpStatusCode.BadGateway => "https://api.hgv.com/errors/bad-gateway",
                    HttpStatusCode.TooManyRequests => "https://api.hgv.com/errors/too-many-requests",
                    HttpStatusCode.Continue => throw new NotImplementedException(),
                    HttpStatusCode.SwitchingProtocols => throw new NotImplementedException(),
                    HttpStatusCode.Processing => throw new NotImplementedException(),
                    HttpStatusCode.EarlyHints => throw new NotImplementedException(),
                    HttpStatusCode.OK => throw new NotImplementedException(),
                    HttpStatusCode.Created => throw new NotImplementedException(),
                    HttpStatusCode.Accepted => throw new NotImplementedException(),
                    HttpStatusCode.NonAuthoritativeInformation => throw new NotImplementedException(),
                    HttpStatusCode.NoContent => throw new NotImplementedException(),
                    HttpStatusCode.ResetContent => throw new NotImplementedException(),
                    HttpStatusCode.PartialContent => throw new NotImplementedException(),
                    HttpStatusCode.MultiStatus => throw new NotImplementedException(),
                    HttpStatusCode.AlreadyReported => throw new NotImplementedException(),
                    HttpStatusCode.IMUsed => throw new NotImplementedException(),
                    HttpStatusCode.Ambiguous => throw new NotImplementedException(),
                    HttpStatusCode.Moved => throw new NotImplementedException(),
                    HttpStatusCode.Found => throw new NotImplementedException(),
                    HttpStatusCode.RedirectMethod => throw new NotImplementedException(),
                    HttpStatusCode.NotModified => throw new NotImplementedException(),
                    HttpStatusCode.UseProxy => throw new NotImplementedException(),
                    HttpStatusCode.Unused => throw new NotImplementedException(),
                    HttpStatusCode.RedirectKeepVerb => throw new NotImplementedException(),
                    HttpStatusCode.PermanentRedirect => throw new NotImplementedException(),
                    HttpStatusCode.MethodNotAllowed => throw new NotImplementedException(),
                    HttpStatusCode.NotAcceptable => throw new NotImplementedException(),
                    HttpStatusCode.ProxyAuthenticationRequired => throw new NotImplementedException(),
                    HttpStatusCode.RequestTimeout => throw new NotImplementedException(),
                    HttpStatusCode.Conflict => throw new NotImplementedException(),
                    HttpStatusCode.Gone => throw new NotImplementedException(),
                    HttpStatusCode.LengthRequired => throw new NotImplementedException(),
                    HttpStatusCode.PreconditionFailed => throw new NotImplementedException(),
                    HttpStatusCode.RequestEntityTooLarge => throw new NotImplementedException(),
                    HttpStatusCode.RequestUriTooLong => throw new NotImplementedException(),
                    HttpStatusCode.UnsupportedMediaType => throw new NotImplementedException(),
                    HttpStatusCode.RequestedRangeNotSatisfiable => throw new NotImplementedException(),
                    HttpStatusCode.ExpectationFailed => throw new NotImplementedException(),
                    HttpStatusCode.MisdirectedRequest => throw new NotImplementedException(),
                    HttpStatusCode.UnprocessableEntity => throw new NotImplementedException(),
                    HttpStatusCode.Locked => throw new NotImplementedException(),
                    HttpStatusCode.FailedDependency => throw new NotImplementedException(),
                    HttpStatusCode.UpgradeRequired => throw new NotImplementedException(),
                    HttpStatusCode.PreconditionRequired => throw new NotImplementedException(),
                    HttpStatusCode.RequestHeaderFieldsTooLarge => throw new NotImplementedException(),
                    HttpStatusCode.UnavailableForLegalReasons => throw new NotImplementedException(),
                    HttpStatusCode.ServiceUnavailable => throw new NotImplementedException(),
                    HttpStatusCode.GatewayTimeout => throw new NotImplementedException(),
                    HttpStatusCode.HttpVersionNotSupported => throw new NotImplementedException(),
                    HttpStatusCode.VariantAlsoNegotiates => throw new NotImplementedException(),
                    HttpStatusCode.InsufficientStorage => throw new NotImplementedException(),
                    HttpStatusCode.LoopDetected => throw new NotImplementedException(),
                    HttpStatusCode.NotExtended => throw new NotImplementedException(),
                    HttpStatusCode.NetworkAuthenticationRequired => throw new NotImplementedException(),
                    _ => "https://api.hgv.com/errors/unexpected-error"
                },
                Title = statusCode switch
                {
                    HttpStatusCode.BadRequest => "Bad Request",
                    HttpStatusCode.Unauthorized => "Unauthorized",
                    HttpStatusCode.PaymentRequired => "Payment Required",
                    HttpStatusCode.Forbidden => "Forbidden",
                    HttpStatusCode.NotFound => "Not Found",
                    HttpStatusCode.NotAcceptable => "Not Acceptable",
                    HttpStatusCode.TooManyRequests => "Too Many Requests",
                    HttpStatusCode.InternalServerError => "Internal Server Error",
                    HttpStatusCode.NotImplemented => "Not Implemented",
                    HttpStatusCode.BadGateway => "Bad Gateway",
                    HttpStatusCode.Continue => throw new NotImplementedException(),
                    HttpStatusCode.SwitchingProtocols => throw new NotImplementedException(),
                    HttpStatusCode.Processing => throw new NotImplementedException(),
                    HttpStatusCode.EarlyHints => throw new NotImplementedException(),
                    HttpStatusCode.OK => throw new NotImplementedException(),
                    HttpStatusCode.Created => throw new NotImplementedException(),
                    HttpStatusCode.Accepted => throw new NotImplementedException(),
                    HttpStatusCode.NonAuthoritativeInformation => throw new NotImplementedException(),
                    HttpStatusCode.NoContent => throw new NotImplementedException(),
                    HttpStatusCode.ResetContent => throw new NotImplementedException(),
                    HttpStatusCode.PartialContent => throw new NotImplementedException(),
                    HttpStatusCode.MultiStatus => throw new NotImplementedException(),
                    HttpStatusCode.AlreadyReported => throw new NotImplementedException(),
                    HttpStatusCode.IMUsed => throw new NotImplementedException(),
                    HttpStatusCode.Ambiguous => throw new NotImplementedException(),
                    HttpStatusCode.Moved => throw new NotImplementedException(),
                    HttpStatusCode.Found => throw new NotImplementedException(),
                    HttpStatusCode.RedirectMethod => throw new NotImplementedException(),
                    HttpStatusCode.NotModified => throw new NotImplementedException(),
                    HttpStatusCode.UseProxy => throw new NotImplementedException(),
                    HttpStatusCode.Unused => throw new NotImplementedException(),
                    HttpStatusCode.RedirectKeepVerb => throw new NotImplementedException(),
                    HttpStatusCode.PermanentRedirect => throw new NotImplementedException(),
                    HttpStatusCode.MethodNotAllowed => throw new NotImplementedException(),
                    HttpStatusCode.ProxyAuthenticationRequired => throw new NotImplementedException(),
                    HttpStatusCode.RequestTimeout => throw new NotImplementedException(),
                    HttpStatusCode.Conflict => throw new NotImplementedException(),
                    HttpStatusCode.Gone => throw new NotImplementedException(),
                    HttpStatusCode.LengthRequired => throw new NotImplementedException(),
                    HttpStatusCode.PreconditionFailed => throw new NotImplementedException(),
                    HttpStatusCode.RequestEntityTooLarge => throw new NotImplementedException(),
                    HttpStatusCode.RequestUriTooLong => throw new NotImplementedException(),
                    HttpStatusCode.UnsupportedMediaType => throw new NotImplementedException(),
                    HttpStatusCode.RequestedRangeNotSatisfiable => throw new NotImplementedException(),
                    HttpStatusCode.ExpectationFailed => throw new NotImplementedException(),
                    HttpStatusCode.MisdirectedRequest => throw new NotImplementedException(),
                    HttpStatusCode.UnprocessableEntity => throw new NotImplementedException(),
                    HttpStatusCode.Locked => throw new NotImplementedException(),
                    HttpStatusCode.FailedDependency => throw new NotImplementedException(),
                    HttpStatusCode.UpgradeRequired => throw new NotImplementedException(),
                    HttpStatusCode.PreconditionRequired => throw new NotImplementedException(),
                    HttpStatusCode.RequestHeaderFieldsTooLarge => throw new NotImplementedException(),
                    HttpStatusCode.UnavailableForLegalReasons => throw new NotImplementedException(),
                    HttpStatusCode.ServiceUnavailable => throw new NotImplementedException(),
                    HttpStatusCode.GatewayTimeout => throw new NotImplementedException(),
                    HttpStatusCode.HttpVersionNotSupported => throw new NotImplementedException(),
                    HttpStatusCode.VariantAlsoNegotiates => throw new NotImplementedException(),
                    HttpStatusCode.InsufficientStorage => throw new NotImplementedException(),
                    HttpStatusCode.LoopDetected => throw new NotImplementedException(),
                    HttpStatusCode.NotExtended => throw new NotImplementedException(),
                    HttpStatusCode.NetworkAuthenticationRequired => throw new NotImplementedException(),
                    _ => "Unexpected Error"
                },
                Status = (int)statusCode,
                Detail = statusCode switch
                {
                    HttpStatusCode.BadRequest => "Invalid or missing parameters.",
                    HttpStatusCode.Unauthorized => "Missing or invalid authentication credentials.",
                    HttpStatusCode.PaymentRequired => "A valid subscription is required to access this resource.",
                    HttpStatusCode.Forbidden => "You do not have permission to access this resource.",
                    HttpStatusCode.NotFound => "No properties found matching the specified criteria.",
                    HttpStatusCode.NotAcceptable => "The requested content type is not supported.",
                    HttpStatusCode.TooManyRequests => "Rate limit exceeded. Please try again after the retry-after period.",
                    HttpStatusCode.InternalServerError => "An unexpected error occurred on the server.",
                    HttpStatusCode.NotImplemented => "The requested functionality is not supported.",
                    HttpStatusCode.BadGateway => "Invalid response from an upstream server.",
                    HttpStatusCode.Continue => throw new NotImplementedException(),
                    HttpStatusCode.SwitchingProtocols => throw new NotImplementedException(),
                    HttpStatusCode.Processing => throw new NotImplementedException(),
                    HttpStatusCode.EarlyHints => throw new NotImplementedException(),
                    HttpStatusCode.OK => throw new NotImplementedException(),
                    HttpStatusCode.Created => throw new NotImplementedException(),
                    HttpStatusCode.Accepted => throw new NotImplementedException(),
                    HttpStatusCode.NonAuthoritativeInformation => throw new NotImplementedException(),
                    HttpStatusCode.NoContent => throw new NotImplementedException(),
                    HttpStatusCode.ResetContent => throw new NotImplementedException(),
                    HttpStatusCode.PartialContent => throw new NotImplementedException(),
                    HttpStatusCode.MultiStatus => throw new NotImplementedException(),
                    HttpStatusCode.AlreadyReported => throw new NotImplementedException(),
                    HttpStatusCode.IMUsed => throw new NotImplementedException(),
                    HttpStatusCode.Ambiguous => throw new NotImplementedException(),
                    HttpStatusCode.Moved => throw new NotImplementedException(),
                    HttpStatusCode.Found => throw new NotImplementedException(),
                    HttpStatusCode.RedirectMethod => throw new NotImplementedException(),
                    HttpStatusCode.NotModified => throw new NotImplementedException(),
                    HttpStatusCode.UseProxy => throw new NotImplementedException(),
                    HttpStatusCode.Unused => throw new NotImplementedException(),
                    HttpStatusCode.RedirectKeepVerb => throw new NotImplementedException(),
                    HttpStatusCode.PermanentRedirect => throw new NotImplementedException(),
                    HttpStatusCode.MethodNotAllowed => throw new NotImplementedException(),
                    HttpStatusCode.ProxyAuthenticationRequired => throw new NotImplementedException(),
                    HttpStatusCode.RequestTimeout => throw new NotImplementedException(),
                    HttpStatusCode.Conflict => throw new NotImplementedException(),
                    HttpStatusCode.Gone => throw new NotImplementedException(),
                    HttpStatusCode.LengthRequired => throw new NotImplementedException(),
                    HttpStatusCode.PreconditionFailed => throw new NotImplementedException(),
                    HttpStatusCode.RequestEntityTooLarge => throw new NotImplementedException(),
                    HttpStatusCode.RequestUriTooLong => throw new NotImplementedException(),
                    HttpStatusCode.UnsupportedMediaType => throw new NotImplementedException(),
                    HttpStatusCode.RequestedRangeNotSatisfiable => throw new NotImplementedException(),
                    HttpStatusCode.ExpectationFailed => throw new NotImplementedException(),
                    HttpStatusCode.MisdirectedRequest => throw new NotImplementedException(),
                    HttpStatusCode.UnprocessableEntity => throw new NotImplementedException(),
                    HttpStatusCode.Locked => throw new NotImplementedException(),
                    HttpStatusCode.FailedDependency => throw new NotImplementedException(),
                    HttpStatusCode.UpgradeRequired => throw new NotImplementedException(),
                    HttpStatusCode.PreconditionRequired => throw new NotImplementedException(),
                    HttpStatusCode.RequestHeaderFieldsTooLarge => throw new NotImplementedException(),
                    HttpStatusCode.UnavailableForLegalReasons => throw new NotImplementedException(),
                    HttpStatusCode.ServiceUnavailable => throw new NotImplementedException(),
                    HttpStatusCode.GatewayTimeout => throw new NotImplementedException(),
                    HttpStatusCode.HttpVersionNotSupported => throw new NotImplementedException(),
                    HttpStatusCode.VariantAlsoNegotiates => throw new NotImplementedException(),
                    HttpStatusCode.InsufficientStorage => throw new NotImplementedException(),
                    HttpStatusCode.LoopDetected => throw new NotImplementedException(),
                    HttpStatusCode.NotExtended => throw new NotImplementedException(),
                    HttpStatusCode.NetworkAuthenticationRequired => throw new NotImplementedException(),
                    _ => "An unexpected error occurred."
                },
                Instance = $"https://api.hgv.com/errors/{correlationId}"
            };
        }
    }
}
