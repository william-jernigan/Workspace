using OneExchangeOrderService.Application.Mapping;
using OneExchangeOrderService.Domain.Constants;
using OneExchangeOrderService.Domain.Models;
using OneExchangeOrderService.Domain.Models.PostOrderModels;
// Aliases for brevity
using OrderEntity = OneExchangeOrderService.Domain.Entities.ExchangeOrder.Order;
using OrderItemEntity = OneExchangeOrderService.Domain.Entities.ExchangeOrder.OrderItem;


namespace Application.UnitTests.Mapping
{
    [TestClass]
    public class MappingOrderToEventhubRequestTests
    {
        private static OrderEntity MakeOrderWithoutPropertyNameAttr()
        {
            var item = new OrderItemEntity
            {
                Id = Guid.NewGuid(),
                Sequence = 1,
                ItemId = Guid.NewGuid(),
                ItemDescription = "Unit A",
                Quantity = 1,
                UnitPrice = 100m,
                UnitPriceCurrency = "USD",
                TaxCurrency = "USD",
                TaxAmount = 5m,
                Memo = "item memo",
                OrderItemAttributes =
                [
                    new() { AttributeType = "CHECK_IN_DATE", AttributeValue = "2026-01-10" }
                ]
            };

            return new OrderEntity
            {
                Id = Guid.NewGuid(),
                OrderType = "BOOKING",
                OrderNumber = "ORD123",
                OrderDate = DateTime.UtcNow,
                Currency = "USD",
                Subtotal = 100m,
                TaxCurrency = "USD",
                TaxAmount = 5m,
                FeeCurrency = "USD",
                OrderFee = 2m,
                Memo = "order memo",
                ExternalSourceSystem = "SYS",
                ExternalReferenceId = "EXT-1",
                OrderItems = [item]
            };
        }

        private static OrderEntity MakeOrderWithExistingPropertyNameAttr(string existingName)
        {
            var item = new OrderItemEntity
            {
                Id = Guid.NewGuid(),
                Sequence = 1,
                ItemId = Guid.NewGuid(),
                ItemDescription = "Unit B",
                Quantity = 1,
                UnitPrice = 200m,
                UnitPriceCurrency = "USD",
                TaxCurrency = "USD",
                TaxAmount = 10m,
                Memo = "item memo",
                OrderItemAttributes =
                [
                    new()
                    {
                        AttributeType = OneExchangeOrderServiceConstants.PropertyNameAttributeTypeString,
                        AttributeValue = existingName
                    }
                ]
            };

            return new OrderEntity
            {
                Id = Guid.NewGuid(),
                OrderType = "BOOKING",
                OrderNumber = "ORD999",
                OrderDate = DateTime.UtcNow,
                Currency = "USD",
                Subtotal = 200m,
                TaxCurrency = "USD",
                TaxAmount = 10m,
                FeeCurrency = "USD",
                OrderFee = 3m,
                Memo = "order memo",
                ExternalSourceSystem = "SYS",
                ExternalReferenceId = "EXT-9",
                OrderItems = [item]
            };
        }

        private static OrderReservationRequest MakeRequest(string itemCode = "PARTNER=GP;UNIT=UT1") => new()
        {
            TradingAgreementCode = "TA-001",
            WaiveFee = false,
            CustomerProfile = new ProfileRequest
            {
            },
            SalesPersonProfile = new ProfileRequest
            {
                FirstName = "Alice",
                LastName = "Smith",
                DigitalAddresses =
                [
                    new()
                    {
                        Sequence = 1,
                        DigitalAddressType = "EMAIL",
                        DigitalAddressDetail = "alice@example.com"
                    }
                ]
            },
            Guests = null,
            Payments =
            [
                new()
                {
                    PaymentAccountToken = "tok_abc",
                    ExpirationDate = "1227"
                }
            ],
            OrderItems =
            [
                new()
                {
                    ItemCode = itemCode
                }
            ]
        };

        [TestMethod]
        public void MapToOrderRequestedEventHappyPathMapsCoreFieldsAndAppendsPropertyAttributes()
        {
            // arrange
            var order = MakeOrderWithoutPropertyNameAttr();
            var request = MakeRequest();

            var propertyName = "Resort Name";
            var propertyPhone = "555-1010";
            var propertyAddr = "1 Code St, City, ST 00000";
            var memberLevel = "GOLD";
            var propertyInfo = "Important info";
            var unitType = "1BR";
            List<CurrencyEquivalent>? currencyEquivalent = [
                new() { AmountType = "SUBTOTAL", Currency = "USD", Amount = 100m }
            ];

            // act
            var ev = MappingOrderToEventhubRequest.MapToOrderRequestedEvent(
                order, request,
                propertyName, propertyPhone, propertyAddr, memberLevel, propertyInfo, unitType,
                currencyEquivalent
            );

            // assert
            Assert.IsNotNull(ev);
            Assert.IsNotNull(ev.Data);
            Assert.AreEqual(order.Id.ToString(), ev.Data.OrderId);
            Assert.AreEqual(order.OrderNumber, ev.Data.OrderNumber);
            Assert.AreEqual("PENDING", ev.Data.OrderStatus); // hardcoded in mapper
            Assert.AreEqual(order.Currency, ev.Data.Currency);
            Assert.AreEqual(order.Subtotal, ev.Data.Subtotal);
            Assert.AreEqual(order.TaxAmount, ev.Data.TaxAmount);
            Assert.AreEqual(order.Memo, ev.Data.Memo);
            Assert.AreEqual(order.ExternalSourceSystem, ev.Data.ExternalOrderReference.ExternalSourceSystem);
            Assert.AreEqual(order.ExternalReferenceId, ev.Data.ExternalOrderReference.ExternalReferenceId);
            Assert.AreEqual(request.TradingAgreementCode, ev.Data.TradingAgreementCode);
            Assert.AreEqual(request.WaiveFee, ev.Data.WaiveFee);


            Assert.IsNotNull(ev.Data.Payments);
            Assert.AreEqual(1, ev.Data.Payments.Count);
            Assert.AreEqual("tok_abc", ev.Data.Payments[0].PaymentAccountToken);
            Assert.AreEqual("1227", ev.Data.Payments[0].ExpirationDate);


            Assert.IsNotNull(ev.Data.OrderItems);
            Assert.AreEqual(1, ev.Data.OrderItems.Count);

            var outItem = ev.Data.OrderItems[0];
            Assert.AreEqual(order.OrderItems.First().ItemDescription, outItem.ItemDescription);
            Assert.AreEqual(order.OrderItems.First().UnitPrice, outItem.UnitPrice);

            var attrs = outItem.Attributes;
            Assert.IsNotNull(attrs);


            Assert.IsTrue(attrs.Any(a => a.AttributeType == OneExchangeOrderServiceConstants.PropertyNameAttributeTypeString &&
                                         a.AttributeValue == propertyName));
            Assert.IsTrue(attrs.Any(a => a.AttributeType == OneExchangeOrderServiceConstants.PropertyPhoneAttributeTypeString &&
                                         a.AttributeValue == propertyPhone));
            Assert.IsTrue(attrs.Any(a => a.AttributeType == OneExchangeOrderServiceConstants.PropertyAddressAttributeTypeString &&
                                         a.AttributeValue == propertyAddr));
            Assert.IsTrue(attrs.Any(a => a.AttributeType == OneExchangeOrderServiceConstants.MemberShipLevelAttributeTypeString &&
                                         a.AttributeValue == memberLevel));
            Assert.IsTrue(attrs.Any(a => a.AttributeType == OneExchangeOrderServiceConstants.PropertyUnitTypeString &&
                                         a.AttributeValue == unitType));
            Assert.IsTrue(attrs.Any(a => a.AttributeType == OneExchangeOrderServiceConstants.PropertyImportantInfoString &&
                                         a.AttributeValue == propertyInfo));

            // currency equivalents passed through
            Assert.AreSame(currencyEquivalent, ev.Data.CurrencyEquivalent);
        }

        [TestMethod]
        public void MapToOrderRequestedEventDoesNotDuplicatePropertyNameIfAlreadyPresent()
        {
            // arrange
            var existingName = "Existing Resort";
            var order = MakeOrderWithExistingPropertyNameAttr(existingName);
            var request = MakeRequest();


            var ev = MappingOrderToEventhubRequest.MapToOrderRequestedEvent(
                order, request,
                propertyName: "New Resort",
                propertyPhone: null!,
                propertyAddress: null!,
                memberShipLevel: null!,
                propertyInfo: null!,
                unitType: null!,
                currencyEquivalent: null
            );

            // assert
            var attrs = ev.Data.OrderItems[0].Attributes;
            var propNameAttrs = attrs.Where(a =>
                string.Equals(a.AttributeType, OneExchangeOrderServiceConstants.PropertyNameAttributeTypeString, StringComparison.OrdinalIgnoreCase)
            ).ToList();

            Assert.AreEqual(1, propNameAttrs.Count, "Property name attribute should not be duplicated.");
            Assert.AreEqual(existingName, propNameAttrs[0].AttributeValue, "Original value should be preserved.");
        }

        [TestMethod]
        public void MapToOrderRequestedEventHandlesNullListsInRequest()
        {
            // arrange
            var order = MakeOrderWithoutPropertyNameAttr();
            var request = new OrderReservationRequest
            {
                TradingAgreementCode = "TA-NULLS",
                WaiveFee = true,
                CustomerProfile = new ProfileRequest(),
                SalesPersonProfile = null,
                Guests = null,
                Payments = null,
                OrderItems = null
            };

            // act
            var ev = MappingOrderToEventhubRequest.MapToOrderRequestedEvent(
                order, request,
                propertyName: null!, propertyPhone: null!, propertyAddress: null!,
                memberShipLevel: null!, propertyInfo: null!, unitType: null!,
                currencyEquivalent: null
            );

            // assert
            Assert.IsNotNull(ev);
            Assert.AreEqual(order.Id.ToString(), ev.Data.OrderId);
            Assert.AreEqual("TA-NULLS", ev.Data.TradingAgreementCode);
            Assert.AreEqual("PENDING", ev.Data.OrderStatus);


            Assert.IsNotNull(ev.Data.Payments);
            Assert.AreEqual(1, ev.Data.Payments.Count);
            Assert.IsNull(ev.Data.Payments[0].PaymentAccountToken);
            Assert.IsNull(ev.Data.Payments[0].ExpirationDate);
        }
    }
}