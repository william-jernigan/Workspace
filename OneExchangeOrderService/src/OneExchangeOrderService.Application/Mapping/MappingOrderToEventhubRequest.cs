using OneExchangeOrderService.Domain.Constants;
using OneExchangeOrderService.Domain.Entities.ExchangeOrder;
using OneExchangeOrderService.Domain.Models;
using OneExchangeOrderService.Domain.Models.Eventhub.OrderReservation;
using OneExchangeOrderService.Domain.Models.PostOrderModels;

namespace OneExchangeOrderService.Application.Mapping
{
    public static class MappingOrderToEventhubRequest
    {
        public static OrderRequestedEvent MapToOrderRequestedEvent(Order order, OrderReservationRequest request,
           string propertyName, string propertyPhone, string propertyAddress, string memberShipLevel, string propertyInfo, string unitType, List<CurrencyEquivalent>? currencyEquivalent)
        {
            var eventPayload = new OrderRequestedEvent
            {
                Data = new OrderReservationEventhubData
                {
                    SystemMeta = new SystemMetaEvent
                    {
                        TenantID = "BGV", // Derive this if dynamic
                        InsertedBy = "OneXChange",
                        Status = true,
                        StatusReason = "Active"
                    },
                    OrderId = order.Id.ToString(),
                    TradingAgreementCode = request.TradingAgreementCode,
                    OrderType = order.OrderType,
                    OrderNumber = order.OrderNumber,
                    OrderStatus = "PENDING",
                    OrderDate = order.OrderDate,
                    CancelDate = null,
                    Currency = order.Currency,
                    Subtotal = order.Subtotal ?? 0,
                    TaxCurrency = order.TaxCurrency,
                    TaxAmount = order.TaxAmount ?? 0,
                    FeeCurrency = order.FeeCurrency,
                    OrderFee = order.OrderFee,
                    Memo = order.Memo,
                    WaiveFee = request.WaiveFee,
                    ExternalOrderReference = new ExternalOrderReferenceEvent
                    {
                        ExternalSourceSystem = order.ExternalSourceSystem,
                        ExternalReferenceId = order.ExternalReferenceId
                    },
                    CustomerProfile = new ProfileEvent
                    {
                        Phones = request.CustomerProfile.Phones?.Select(p => new PhoneEvent { Number = p.Number }).ToList(),
                        DigitalAddresses = request.CustomerProfile.DigitalAddresses?.Select(d => new DigitalAddressEvent
                        {
                            Sequence = d.Sequence,
                            DigitalAddressType = d.DigitalAddressType,
                            DigitalAddress = d.DigitalAddressDetail
                        }).ToList(),
                        Identifiers = request.CustomerProfile.Identifiers?.Select(i => new IdentifierAttributesEvent
                        {
                            IdentifierType = i.IdentifierType,
                            IdentifierSource = i.IdentifierSource,
                            Identifier = i.Identifier
                        }).ToList()
                    },
                    SalesPersonProfile = new ProfileEvent
                    {
                        FirstName = request.SalesPersonProfile?.FirstName,
                        LastName = request.SalesPersonProfile?.LastName,
                        DigitalAddresses = request.SalesPersonProfile?.DigitalAddresses?.Select(d => new DigitalAddressEvent
                        {
                            DigitalAddressType = d.DigitalAddressType,
                            DigitalAddress = d.DigitalAddressDetail
                        }).ToList()
                    },
                    Guests = request.Guests?.Select(g => new ProfileEvent
                    {
                        ProfileType = g.ProfileType,
                        FirstName = g.FirstName,
                        LastName = g.LastName,
                        Addresses = g.Addresses?.Select((a, i) => new AddressEvent
                        {
                            Sequence = i + 1,
                            AddressType = a.AddressType,
                            AddressPurpose = a.AddressPurpose,
                            Line1 = a.Line1,
                            Line2 = a.Line2,
                            Line3 = a.Line3,
                            City = a.City,
                            State = a.State,
                            Province = a.Province,
                            PostalCode = a.PostalCode,
                            Country = a.Country
                        }).ToList(),
                        Phones = g.Phones?.Select((p, i) => new PhoneEvent
                        {
                            Sequence = i + 1,
                            PhoneType = p.PhoneType,
                            PhonePurpose = p.PhonePurpose,
                            Number = p.Number,
                            Extension = p.Extension,
                            VerifiedDate = p.VerifiedDate
                        }).ToList(),
                        DigitalAddresses = g.DigitalAddresses?.Select((d, i) => new DigitalAddressEvent
                        {
                            Sequence = i + 1,
                            DigitalAddressType = d.DigitalAddressType,
                            DigitalAddress = d.DigitalAddressDetail,
                            VerifiedDate = d.VerifiedDate
                        }).ToList()
                    }).ToList(),
                    Payments = [
                        new()
                        {

                            PaymentAccountToken = request?.Payments?.FirstOrDefault().PaymentAccountToken,
                            ExpirationDate = request?.Payments?.FirstOrDefault().ExpirationDate

                        }
                    ],
                    OrderItems = order.OrderItems.Select((item, index) =>
                    {
                        var attributes = item.OrderItemAttributes?.Select(a => new OrderItemAttributeEvent
                        {
                            AttributeType = a.AttributeType,
                            AttributeValue = a.AttributeValue
                        }).ToList() ?? [];

                        if (!string.IsNullOrWhiteSpace(propertyName) &&
                            !attributes.Any(a => a.AttributeType.Equals(
                                OneExchangeOrderServiceConstants.PropertyNameAttributeTypeString,
                                StringComparison.OrdinalIgnoreCase)))
                        {
                            attributes.Add(new OrderItemAttributeEvent
                            {
                                AttributeType = OneExchangeOrderServiceConstants.PropertyNameAttributeTypeString,
                                AttributeValue = propertyName
                            });
                        }
                        if (!string.IsNullOrWhiteSpace(propertyPhone))
                        {
                            attributes.Add(new OrderItemAttributeEvent
                            {
                                AttributeType = OneExchangeOrderServiceConstants.PropertyPhoneAttributeTypeString,
                                AttributeValue = propertyPhone
                            });
                        }
                        if (!string.IsNullOrWhiteSpace(propertyAddress))
                        {
                            attributes.Add(new OrderItemAttributeEvent
                            {
                                AttributeType = OneExchangeOrderServiceConstants.PropertyAddressAttributeTypeString,
                                AttributeValue = propertyAddress
                            });
                        }
                        if (!string.IsNullOrWhiteSpace(memberShipLevel))
                        {
                            attributes.Add(new OrderItemAttributeEvent
                            {
                                AttributeType = OneExchangeOrderServiceConstants.MemberShipLevelAttributeTypeString,
                                AttributeValue = memberShipLevel
                            });
                        }
                        if (!string.IsNullOrWhiteSpace(unitType))
                        {
                            attributes.Add(new OrderItemAttributeEvent
                            {
                                AttributeType = OneExchangeOrderServiceConstants.PropertyUnitTypeString,
                                AttributeValue = unitType
                            });
                        }
                        if (!string.IsNullOrWhiteSpace(propertyInfo))
                        {
                            attributes.Add(new OrderItemAttributeEvent
                            {
                                AttributeType = OneExchangeOrderServiceConstants.PropertyImportantInfoString,
                                AttributeValue = propertyInfo
                            });
                        }
                        return new OrderItemEventModelEvent
                        {
                            Id = item.Id,
                            Sequence = item.Sequence,
                            ItemId = item.ItemId,
                            ItemCode = request.OrderItems?.FirstOrDefault()?.ItemCode,
                            ItemDescription = item.ItemDescription,
                            Quantity = item.Quantity,
                            Currency = item.UnitPriceCurrency,
                            UnitPrice = item.UnitPrice,
                            TaxCurrency = item.TaxCurrency,
                            TaxAmount = item.TaxAmount,
                            Memo = item.Memo,
                            Attributes = attributes
                        };
                    }).ToList(),

                    CurrencyEquivalent = currencyEquivalent
                }
            };

            return eventPayload;
        }
    }
}
