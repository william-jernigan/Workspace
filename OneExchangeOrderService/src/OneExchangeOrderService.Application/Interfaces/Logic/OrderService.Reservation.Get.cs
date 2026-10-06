using Microsoft.Extensions.Logging;
using OneExchangeOrderService.Domain.Constants;
using OneExchangeOrderService.Domain.Entities.ExchangeOrder;
using OneExchangeOrderService.Domain.Entities.ExchangeProfile;
using OneExchangeOrderService.Domain.Exceptions;
using OneExchangeOrderService.Domain.Models.V2.Retrieval.Response;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace OneExchangeOrderService.Application.Interfaces.Logic
{
    public partial class OrderService
    {
        public async Task<ReservationGetResponse> GetReservationByIdAsync(string reservationID, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation("{Prefix} Received GetReservationById request for reservation={reservationID}", OneExchangeOrderServiceConstants.OrderServiceReservationPrefix, reservationID);

            if (string.IsNullOrWhiteSpace(reservationID))
            {
                _logger.LogWarning("{Prefix} Reservation ID is null or empty", OneExchangeOrderServiceConstants.OrderServiceReservationPrefix);
                throw new ValidationException("Reservation ID cannot be null or empty.");
            }


            var order = await _unitOfWork.OrderRepository.GetOrderByExternalReferenceIdAsync(reservationID, ct);

            if (order == null)
            {
                _logger.LogWarning("{Prefix} No reservation found for ID={ReservationId}", OneExchangeOrderServiceConstants.OrderServiceReservationPrefix, reservationID);
                throw new DataNotFoundException($"Reservation with ID {reservationID} not found.");
            }

            var profileIds = order.OrderGuests.Select(g => g.ProfileId).ToList();
            var profiles = await _unitOfWork.ProfileRepository.GetOrderGuestsDetailsAsync(profileIds);

            if (profiles == null)
            {
                _logger.LogWarning("{Prefix} No profile found for the profile id", OneExchangeOrderServiceConstants.OrderServiceReservationPrefix);
                throw new DataNotFoundException("profile not found");
            }

            var profile = profiles.FirstOrDefault();
            var now = DateTimeOffset.UtcNow;

            var response = new ReservationGetResponse
            {
                Id = order.ExternalReferenceId,
                Source = OneExchangeOrderServiceConstants.OrderServiceSource,
                Specversion = OneExchangeOrderServiceConstants.CloudEventSpecVersion,
                Type = OneExchangeOrderServiceConstants.ReservationGetResponseType,
                Datacontenttype = OneExchangeOrderServiceConstants.ApplicationJson,
                Dataschema = OneExchangeOrderServiceConstants.ReservationGetSchema,
                Subject = $"reservation/{order.ExternalReferenceId}",
                Time = now,
                Ispartial = false,
                Data = new ReservationRetrievalData
                {
                    SystemMeta = new SystemMeta
                    {
                        TenantID = order.TenantId.ToString(),
                        InsertedDateTime = order.InsertedDateTime,
                        InsertedBy = order.InsertedBy,
                        UpdatedDateTime = order.UpdatedDateTime,
                        UpdatedBy = order.UpdatedBy,
                        Status = order.Status,
                        StatusReason = order.StatusReason
                    },
                    ExternalReference = new ExternalReference
                    {
                        SourceSystem = order.ExternalSourceSystem,
                        TargetSystem = OneExchangeOrderServiceConstants.TargetSystem,
                        SourceReferenceId = "",
                        TargetReferenceId = order.ExternalReferenceId
                    },
                    BookingItems = order.OrderItems.Select(item => new RetrievalBookingItem
                    {
                        ItemCode = item.ItemCode,
                        ItemClass = "UNIT",
                        ParentCode = string.Join("-", item.ItemCode.Split('-').Take(2)),
                        Name = item.ItemDescription,
                        Description = item.ItemDescription,
                        ItemAttributes = [],
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
                    }).ToList()
                }
            };
            return response;
        }
        private static GuestInfo MapGuestInfo(Profile profile, Order order)
        {
            if (profile == null)
            {
                return new GuestInfo
                {
                    FirstName = order.CustomerFirstName ?? string.Empty,
                    LastName = order.CustomerLastName ?? string.Empty,
                    PostalAddresses = [],
                    DigitalAddresses = [],
                    PhoneNumbers = []
                };
            }

            return new GuestInfo
            {
                FirstName = profile.FirstName ?? string.Empty,
                LastName = profile.LastName ?? string.Empty,
                PostalAddresses = profile.Addresses?.Select(a => new PostalAddress
                {
                    Sequence = a.Sequence,
                    AddressType = a.AddressType ?? string.Empty,
                    Line1 = a.Line1 ?? string.Empty,
                    Line2 = a.Line2 ?? a.Line3 ?? string.Empty,
                    City = a.City ?? string.Empty,
                    State = a.State ?? a.Province ?? string.Empty,
                    PostalCode = a.Postalcode ?? string.Empty,
                    Country = a.Country ?? string.Empty
                }).ToList() ?? [],

                DigitalAddresses = profile.DigitalAddresses?.Select(d => new Domain.Models.V2.Retrieval.Response.DigitalAddress
                {
                    Sequence = d.Sequence,
                    DigitalAddressType = d.DigitalAddressType ?? string.Empty,
                    DigitalAddressValue = d.DigitalAddress1 ?? string.Empty
                }).ToList() ?? [],

                PhoneNumbers = profile.Phones?.Select(p => new PhoneNumber
                {
                    Sequence = p.Sequence,
                    PhoneType = p.PhoneType ?? string.Empty,
                    Number = p.Number ?? string.Empty
                }).ToList() ?? []
            };
        }
        private static int? GetInt(OrderItem item, string key)
        {
            var val = item.OrderItemAttributes?.FirstOrDefault(a => a.AttributeType == key)?.AttributeValue;
            return int.TryParse(val, out var result) ? result : null;
        }
    }
}
