using Microsoft.Extensions.Logging;
using OneExchangeOrderService.Domain.Constants;
using OneExchangeOrderService.Domain.Entities.ExchangeOrder;
using OneExchangeOrderService.Domain.Entities.ExchangeProfile;
using OneExchangeOrderService.Domain.Models.V2.Retrieval.Request;
using OneExchangeOrderService.Domain.Models.V2.Retrieval.Response;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;

namespace OneExchangeOrderService.Application.Interfaces.Logic
{
    public partial class OrderService
    {
        public async Task<ReservationSearchResponse> SearchReservationsAsync(
            ReservationSearchRequest request, int page, int pageSize, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();
            const string prefix = "ReservationSearchService";

            if (request == null)
            {
                throw new ValidationException("Request cannot be null.");
            }

            if (page < 1)
            {
                throw new ValidationException("page must be greater than or equal to 1.");
            }

            if (pageSize is < 1 or > 100)
            {
                throw new ValidationException("pageSize must be between 1 and 100.");
            }

            _logger.LogInformation("{Prefix} - Received search request ce.id={Id}, src={Src}", prefix, request.Id, request.Source);

            ValidateReservationSearchRequest(request);

            var criteria = request.Data.SearchCriteria;
            var reservationId = string.Empty;
            var memberId = string.Empty;
            IEnumerable<Profile>? profilesByMemberId = null;
            List<Guid>? profileIds = null;

            if (criteria.SourceReservationIds != null && criteria.SourceReservationIds.Count != 0)
            {
                reservationId = criteria.SourceReservationIds?.Where(x => x.SourceSystem.Equals("EXCHANGE", StringComparison.OrdinalIgnoreCase)).FirstOrDefault()?.Id;
            }

            if (criteria.SourceMemberIds != null && criteria.SourceMemberIds.Count != 0)
            {
                memberId = criteria.SourceMemberIds?.Where(x => x.SourceSystem.Equals("EXCHANGE", StringComparison.OrdinalIgnoreCase)).FirstOrDefault()?.Id;
                profilesByMemberId = await _unitOfWork.ProfileRepository.GetProfilesByExternalReferenceIdAsync(memberId);
                profileIds = profilesByMemberId?.Select(p => p.Id).ToList();
                if (profileIds == null || profileIds.Count == 0)
                {
                    _logger.LogInformation("{Prefix} - No profiles found for memberId={MemberId}", prefix, memberId);
                    return BuildEmptyResponse(request);
                }
            }

            var (orders, totalCount) = await _unitOfWork.OrderRepository.SearchOrdersAsync(
                criteria.Status,
                reservationId,
                profileIds,
                criteria.ReservationDate,
                criteria.ReservationDateInterval,
                criteria.ArrivalDate,
                criteria.ArrivalDateInterval,
                page,
                pageSize,
                criteria.OrderBy?.Field,
                criteria.OrderBy?.Direction,
                ct);

            if (orders == null || !orders.Any())
            {
                _logger.LogInformation("{Prefix} - No matching reservations found", prefix);
                return BuildEmptyResponse(request);
            }

            var allProfileIds = orders
                .SelectMany(o => o.OrderGuests.Select(g => g.ProfileId))
                .Distinct()
                .ToList();

            var profiles = await _unitOfWork.ProfileRepository.GetOrderGuestsDetailsAsync(allProfileIds);

            var profileMap = profiles.ToDictionary(p => p.Id, p => p);

            var results = MapOrdersToBookingItems(orders, profileMap);

            var totalItems = totalCount;
            var totalPages = pageSize > 0 ? (int)Math.Ceiling((double)totalItems / pageSize) : 0;

            var response = new ReservationSearchResponse
            {
                Id = request.Id,
                Source = request.Source,
                Specversion = request.Specversion,
                Type = "Reservation-Search-Response",
                Time = DateTimeOffset.UtcNow,
                IsPartial = false,
                Data = new ReservationSearchResponseData
                {
                    SystemMeta = new SystemMeta
                    {
                        TenantID = request.Data.SystemMeta.TenantID,
                        InsertedDateTime = request.Data.SystemMeta.InsertedDateTime,
                        InsertedBy = request.Data.SystemMeta.InsertedBy,
                        UpdatedDateTime = request.Data.SystemMeta.UpdatedDateTime,
                        UpdatedBy = request.Data.SystemMeta.UpdatedBy,
                        Status = request.Data.SystemMeta.Status,
                        StatusReason = request.Data.SystemMeta.StatusReason
                    },
                    ExternalReference = new ExternalReference
                    {
                        SourceSystem = "EXCHANGE",
                        TargetSystem = "HGV",
                        SourceReferenceId = "ID",
                        TargetReferenceId = request.Id
                    },
                    Pagination = new Pagination
                    {
                        Page = page,
                        PageSize = pageSize,
                        TotalItems = totalItems,
                        TotalPages = totalPages
                    },
                    Results = results
                }
            };

            sw.Stop();
            _logger.LogInformation("{Prefix} - Completed search in {Elapsed} ms. Returned {Count} results",
                prefix, sw.ElapsedMilliseconds, results.Count);

            return response;
        }

        private static ReservationSearchResponse BuildEmptyResponse(ReservationSearchRequest request)
        {
            var response = new ReservationSearchResponse
            {
                Id = request.Id,
                Source = request.Source,
                Type = "Reservation-Search-Response",
                Time = DateTimeOffset.UtcNow,
                IsPartial = false,
                Data = new ReservationSearchResponseData
                {
                    SystemMeta = new SystemMeta
                    {
                        TenantID = request.Data.SystemMeta.TenantID,
                        InsertedDateTime = request.Data.SystemMeta.InsertedDateTime,
                        InsertedBy = request.Data.SystemMeta.InsertedBy,
                        UpdatedDateTime = request.Data.SystemMeta.UpdatedDateTime,
                        UpdatedBy = request.Data.SystemMeta.UpdatedBy,
                        Status = request.Data.SystemMeta.Status,
                        StatusReason = request.Data.SystemMeta.StatusReason
                    },
                    ExternalReference = new ExternalReference
                    {
                        SourceSystem = request.Source,
                        TargetSystem = "HGV",
                        SourceReferenceId = request.Id
                    },
                    Pagination = new Pagination { Page = 1, PageSize = 0, TotalItems = 0, TotalPages = 0 },
                    Results = []
                }
            };
            return response;
        }

        private static List<RetrievalBookingItem> MapOrdersToBookingItems(IEnumerable<Order> orders, Dictionary<Guid, Profile> profileMap)
        {
            var list = new List<RetrievalBookingItem>();

            foreach (var order in orders)
            {
                var profile = order.OrderGuests
                    .Select(g => profileMap.GetValueOrDefault(g.ProfileId))
                    .FirstOrDefault(p => p != null);

                foreach (var item in order.OrderItems)
                {
                    var parentCode = string.Join("-", item.ItemCode.Split('-').Take(2));

                    var checkInAttr = item.OrderItemAttributes.FirstOrDefault(a => a.AttributeType == OneExchangeOrderServiceConstants.Checkin);
                    var checkOutAttr = item.OrderItemAttributes.FirstOrDefault(a => a.AttributeType == OneExchangeOrderServiceConstants.Checkout);

                    list.Add(new RetrievalBookingItem
                    {
                        ItemCode = item.ItemCode,
                        ItemClass = "UNIT",
                        ParentCode = parentCode,
                        Name = item.ItemDescription,
                        Description = item.ItemDescription,
                        MemberID = profile?.ExternalReferenceId ?? string.Empty,
                        StartDate = item.OrderItemAttributes?.FirstOrDefault(a => a.AttributeType == OneExchangeOrderServiceConstants.CheckInDateOrderItemAttributeTypeString)?.AttributeValue,
                        EndDate = item.OrderItemAttributes?.FirstOrDefault(a => a.AttributeType == OneExchangeOrderServiceConstants.CheckOutDateOrderItemAttributeTypeString)?.AttributeValue,
                        WeekNumber = 0,
                        Action = "",
                        GuestCounts = new GuestCounts
                        {
                            Adults = GetInt(item, OneExchangeOrderServiceConstants.NumAdultsOrderItemAttributeTypeString),
                            Children = GetInt(item, OneExchangeOrderServiceConstants.NumChildrenOrderItemAttributeTypeString)
                        },
                        IsOwnerBooking = false,
                        PriceLevel = new PriceLevel
                        {
                            Currency = item.UnitPriceCurrency,
                            TotalPrice = item.UnitPrice,
                            Status = true,
                            PriceLevelCode = OneExchangeOrderServiceConstants.PriceLevelStandard,
                            PriceAttributes = []
                        },
                        GuestInfo = MapGuestInfo(profile, order),
                        BookingAttributes = item.OrderItemAttributes?.Select(a => new AttributePair
                        {
                            Attribute = a.AttributeType,
                            Value = a.AttributeValue
                        }).Concat(BuildOrderAttributes(order)).ToList(),
                        BookingComments = []
                    });
                }
            }
            return list;
        }

        private static void ValidateReservationSearchRequest(ReservationSearchRequest request)
        {
            if (request?.Data?.SearchCriteria == null)
            {
                throw new ValidationException("Search criteria is required.");
            }

            if (string.IsNullOrWhiteSpace(request.CorrelationId))
            {
                throw new ValidationException("correlationId is required.");
            }

            var criteria = request.Data.SearchCriteria;

            ValidateTradingAgreement(criteria);
            ValidateReservationIds(criteria.SourceReservationIds);
            ValidateMemberIds(criteria.SourceMemberIds);
            ValidateStatus(criteria.Status);
            ValidateIntervals(criteria);
            ValidateOrderBy(criteria);
            ValidateAtLeastOneFilter(criteria);
        }

        private static void ValidateTradingAgreement(SearchCriteria criteria)
        {
            if (string.IsNullOrWhiteSpace(criteria.TradingAgreementCode))
            {
                throw new ValidationException("tradingAgreementCode is required.");
            }
        }

        private static void ValidateReservationIds(List<SourceIdentifier>? ids)
        {
            if (ids == null)
            {
                return;
            }

            if (ids.Count > 10)
            {
                throw new ValidationException("sourceReservationIds cannot have more than 10 items.");
            }

            foreach (var id in ids)
            {
                ValidateSourceSystemAndId(id, "Reservation id must not exceed 20 characters.");
            }
        }

        private static void ValidateMemberIds(List<SourceIdentifier>? ids)
        {
            if (ids == null)
            {
                return;
            }

            if (ids.Count > 10)
            {
                throw new ValidationException("sourceMemberIds cannot have more than 10 items.");
            }

            foreach (var id in ids)
            {
                ValidateSourceSystemAndId(id, "Member id must not exceed 20 characters.");
            }
        }

        private static void ValidateSourceSystemAndId(SourceIdentifier id, string idLengthMessage)
        {
            if (string.IsNullOrWhiteSpace(id.SourceSystem))
            {
                throw new ValidationException("Each sourceMemberId must have a valid sourceSystem.");
            }

            if (string.IsNullOrWhiteSpace(id.Id))
            {
                throw new ValidationException("Each sourceMemberId must have a valid id.");
            }

            if (id.SourceSystem.Length > 20)
            {
                throw new ValidationException("sourceSystem must not exceed 20 characters.");
            }

            if (id.Id.Length > 20)
            {
                throw new ValidationException(idLengthMessage);
            }
        }

        private static void ValidateStatus(string? status)
        {
            var validStatus = new[] { "CONFIRMED", "CANCELLED", "PENDING" };

            if (!string.IsNullOrWhiteSpace(status) && !validStatus.Contains(status))
            {
                throw new ValidationException("Status must be one of: CONFIRMED, CANCELLED, PENDING.");
            }
        }

        private static void ValidateIntervals(SearchCriteria criteria)
        {
            if (criteria.ReservationDateInterval.HasValue && criteria.ReservationDateInterval < 0)
            {
                throw new ValidationException("reservationDateInterval must be positive.");
            }

            if (criteria.ArrivalDateInterval.HasValue && criteria.ArrivalDateInterval < 0)
            {
                throw new ValidationException("arrivalDateInterval must be positive.");
            }
        }

        private static void ValidateOrderBy(SearchCriteria criteria)
        {
            if (criteria.OrderBy == null)
            {
                return;
            }

            var validFields = new[] { "arrivalDate", "reservationDate" };
            var validDirections = new[] { "asc", "desc" };

            if (!string.IsNullOrWhiteSpace(criteria.OrderBy.Field) &&
                !validFields.Contains(criteria.OrderBy.Field, StringComparer.OrdinalIgnoreCase))
            {
                throw new ValidationException("orderBy.field must be either 'arrivalDate' or 'reservationDate'.");
            }

            if (!string.IsNullOrWhiteSpace(criteria.OrderBy.Direction) &&
                !validDirections.Contains(criteria.OrderBy.Direction, StringComparer.OrdinalIgnoreCase))
            {
                throw new ValidationException("orderBy.direction must be 'asc' or 'desc'.");
            }
        }

        private static void ValidateAtLeastOneFilter(SearchCriteria criteria)
        {
            var hasAtLeastOne =
                (criteria.SourceReservationIds?.Count > 0) ||
                (criteria.SourceMemberIds?.Count > 0) ||
                !string.IsNullOrWhiteSpace(criteria.Status) ||
                criteria.ReservationDate.HasValue ||
                criteria.ArrivalDate.HasValue;

            if (!hasAtLeastOne)
            {
                throw new ValidationException("At least one filter must be provided: reservationId, memberId, status, reservationDate, or arrivalDate.");
            }
        }

        private static List<AttributePair> BuildOrderAttributes(Order order)
        {
            var attributes = new List<AttributePair>();

            if (order.OrderDate.HasValue)
            {
                attributes.Add(new AttributePair
                {
                    Attribute = "RESERVATION_DATE",
                    Value = order.OrderDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                });
            }

            if (!string.IsNullOrWhiteSpace(order.Status.ToString()))
            {
                attributes.Add(new AttributePair
                {
                    Attribute = "ORDER_STATUS",
                    Value = order.OrderStatus.ToString()
                });
            }
            return attributes;
        }
    }
}