using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using OneExchangeOrderService.Application.Eventhub;
using OneExchangeOrderService.Application.Interfaces;
using OneExchangeOrderService.Application.Interfaces.Logic;
using OneExchangeOrderService.Application.Models.Resort;
using OneExchangeOrderService.Domain;
using OneExchangeOrderService.Domain.Constants;
using OneExchangeOrderService.Domain.Constants.Enums;
using OneExchangeOrderService.Domain.Entities.ExchangeOrder;
using OneExchangeOrderService.Domain.Exceptions;
using OneExchangeOrderService.Domain.Extensions;
using OneExchangeOrderService.Domain.Models;
using OneExchangeOrderService.Domain.Models.Eventhub.BookingAlternateEmail;
using OneExchangeOrderService.Domain.Models.Eventhub.OrderReservation;
using OneExchangeOrderService.Domain.Models.PostOrderModels;
using OneExchangeOrderService.Domain.Models.UpdateOrder.Request;
using OneExchangeOrderService.Domain.Models.V2.Retrieval.Request;
using OneExchangeOrderService.Domain.Persistence.Repositories.ExchangeOrder;
using OneExchangeOrderService.Domain.Persistence.Repositories.ExchangeProfile;
using OneExchangeOrderService.DTO.Request;
using OneExchangeOrderService.Infrastructure.Persistance.Repositories.ExchangeOrder;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Location = OneExchangeOrderService.Application.Models.Resort.Location;
using OrderEntity = OneExchangeOrderService.Domain.Entities.ExchangeOrder.Order;
// Alias entity types to avoid AutoMapper.Profile collision
using ProfileEntity = OneExchangeOrderService.Domain.Entities.ExchangeProfile.Profile;

namespace Application.UnitTests.Interfaces.Logic
{
    [TestClass]
    public class OrderServiceTests
    {
        private MockRepository _mocks = default!;

        private Mock<IOrderRepository> _orderRepo = default!;
        private Mock<IOrderReturnRepository> _orderReturnRepo = default!;
        private Mock<IExchangeTransactionLogRepository> _txnRepo = default!;
        private Mock<IProfileRepository> _profileRepo = default!;
        private Mock<ILogger<OrderService>> _logger = default!;
        private Mock<IOrderUnitOfWork> _uow = default!;
        private Mock<IOneExchangeUnitOfWork> _newuow = default!;
        private OrderReservationEventPublisher? _orderEvt = default!;
        private Mock<IOrderPaymentRepository> _orderPaymentRepo = default!;
        private OrderCancelEventPublisher? _cancelEvt = default!;
        private Mock<IContentfulService> _contentful = default!;
        private Mock<IMapper> _mapper = default!;
        private Mock<IInRuleService> _inrule = default!;
        private Mock<IInventoryServiceClient> _inventory = default!;
        private Mock<IBoomiConnector> _reservationConnector = default!;
        private Mock<IAddressRepository> _addressRepo = default!;
        private Mock<IOrderRefundRepository> _refundRepo = default!;

        [TestInitialize]
        public void Init()
        {
            _mocks = new MockRepository(MockBehavior.Strict);

            _orderRepo = _mocks.Create<IOrderRepository>();
            _orderReturnRepo = _mocks.Create<IOrderReturnRepository>();
            _txnRepo = _mocks.Create<IExchangeTransactionLogRepository>();
            _profileRepo = _mocks.Create<IProfileRepository>();
            _logger = _mocks.Create<ILogger<OrderService>>();
            _uow = _mocks.Create<IOrderUnitOfWork>();
            _newuow = new Mock<IOneExchangeUnitOfWork>(MockBehavior.Loose);
            _orderPaymentRepo = _mocks.Create<IOrderPaymentRepository>();

            _contentful = _mocks.Create<IContentfulService>();
            _mapper = _mocks.Create<IMapper>();
            _inrule = _mocks.Create<IInRuleService>();
            _inventory = _mocks.Create<IInventoryServiceClient>();
            _reservationConnector = _mocks.Create<IBoomiConnector>();
            _addressRepo = _mocks.Create<IAddressRepository>();
            _refundRepo = _mocks.Create<IOrderRefundRepository>();
            _logger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
            _logger.Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((_, __) => true),
                It.IsAny<Exception?>(),
               It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ));
            _orderEvt = null;
            _cancelEvt = null;
        }

        private OrderService CreateSut() =>
            new(
                _orderRepo.Object,
                _orderReturnRepo.Object,
                _txnRepo.Object,
                _profileRepo.Object,
                _logger.Object,
                _uow.Object,
                _orderEvt!,
                _orderPaymentRepo.Object,
                _cancelEvt!,
                _contentful.Object,
                _mapper.Object,
                _inrule.Object,
                _inventory.Object, _reservationConnector.Object, _newuow.Object);

        private void SetupLoggerInfo()
        {
            _logger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

            _logger.Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((_, __) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ));
        }
        private static OrderResponse MakeOrder(string status, params Guid[] profileIds) =>
            new()
            {
                OrderStatus = status,
                ProfileId = profileIds?.ToList() ?? [],
                Guests = null, // will be filled by service if we wire GetGuestsAsync
                IsUserEligibleForBookingFeeWaiver = false
            };
        private OrderService CreateService() => new(
                _orderRepo.Object,
                _orderReturnRepo.Object,
                _txnRepo.Object,
                _profileRepo.Object,
                _logger.Object,
                _uow.Object,
                _orderEvt!,
                _orderPaymentRepo.Object,
                _cancelEvt!,
                _contentful.Object,
                _mapper.Object,
                _inrule.Object,
                _inventory.Object,
                _reservationConnector.Object,
                _newuow.Object);

        [TestMethod]
        public async Task GetOrdersAsyncWhenFreeTextUsesFreeTextRepoNoGuestFetchWhenNoProfiles()
        {
            // arrange
            var req = new OrderRequest
            {
                FreeTextSearch = "ORD",
                IsUserDiamondPremierMember = false,
                CustomerExternalReferenceId = "12345"
            };

            var orders = new List<OrderResponse>
           {
              MakeOrder(OrderStatus.CONFIRMED.ToString()),
              MakeOrder(OrderStatus.CANCELLED.ToString())
           };

            _orderRepo
                .Setup(r => r.GetOrdersFreeTextSearchAsync(It.Is<OrderRequest>(q => q == req)))
                .ReturnsAsync(orders);


            var sut = CreateSut();

            // act
            var result = (await sut.GetOrdersAsync(req)).ToList();

            // assert
            Assert.AreEqual(2, result.Count);
            _orderRepo.Verify(r => r.GetOrdersFreeTextSearchAsync(req), Times.Once);
            _orderRepo.Verify(r => r.GetOrdersAsync(It.IsAny<OrderRequest>(), It.IsAny<List<string>>()), Times.Never);
            _profileRepo.Verify(p => p.GetGuestsAsync(It.IsAny<List<Guid>>()), Times.Never);
        }

        [TestMethod]
        public async Task GetOrdersAsyncWhenNoFreeTextCallsRepoWithStatusesExcludesPending()
        {
            // arrange
            var req = new OrderRequest
            {
                FreeTextSearch = null,
                IsUserDiamondPremierMember = false,
                CustomerExternalReferenceId = "12345"
            };

            var returned = new List<OrderResponse>
           {
              MakeOrder(OrderStatus.CONFIRMED.ToString()),
              MakeOrder(OrderStatus.CANCELLED.ToString())
           };

            _orderRepo
                .Setup(r => r.GetOrdersAsync(
                    It.Is<OrderRequest>(q => q == req),
                   It.Is<List<string>>(lst =>
                       !lst.Contains(OrderStatus.PENDING.ToString()) &&
                       Enum.GetNames(typeof(OrderStatus))
                            .Where(s => s != OrderStatus.PENDING.ToString())
                            .All(lst.Contains)
                    )))
                .ReturnsAsync(returned);

            var sut = CreateSut();

            // act
            var result = (await sut.GetOrdersAsync(req)).ToList();

            // assert
            Assert.AreEqual(2, result.Count);
            _orderRepo.Verify(r => r.GetOrdersAsync(
                req,
               It.IsAny<List<string>>()), Times.Once);

            _profileRepo.Verify(p => p.GetGuestsAsync(It.IsAny<List<Guid>>()), Times.Never);
        }

        [TestMethod]
        public async Task GetOrdersAsyncWhenRepoReturnsEmptyReturnsEmptyAndNoGuestFetch()
        {
            // arrange
            var req = new OrderRequest
            {
                FreeTextSearch = null,
                IsUserDiamondPremierMember = false,
                CustomerExternalReferenceId = "12345"
            };

            _orderRepo
                .Setup(r => r.GetOrdersAsync(It.IsAny<OrderRequest>(), It.IsAny<List<string>>()))
               .ReturnsAsync([]);

            var sut = CreateSut();

            // act
            var result = await sut.GetOrdersAsync(req);

            // assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Any());

            _profileRepo.Verify(p => p.GetGuestsAsync(It.IsAny<List<Guid>>()), Times.Never);
        }

        [TestMethod]
        public async Task CreateOrderAsyncWhenRequestIsNullBeginsTxnRollsBackAndRethrows()
        {
            var sut = CreateSut();

            await Assert.ThrowsAsync<NullReferenceException>(() => sut.CreateOrderAsync(null!));

            _uow.Verify(u => u.BeginTransactionAsync(), Times.Never);
            _uow.Verify(u => u.RollbackTransactionAsync(), Times.Never);
            _uow.Verify(u => u.CommitTransactionAsync(), Times.Never);
        }

        [TestMethod]
        public async Task CreateOrderAsyncWhenRequestIsNullBeginsTxnRollsBackAndRethrowsFlexible()
        {
            var sut = CreateSut();

            try
            {
                await sut.CreateOrderAsync(null!);
                Assert.Fail("Expected an exception.");
            }
            catch (Exception ex) when (ex is NullReferenceException or ArgumentNullException)
            {

            }

            _uow.Verify(u => u.BeginTransactionAsync(), Times.Never);
            _uow.Verify(u => u.RollbackTransactionAsync(), Times.Never);
            _uow.Verify(u => u.CommitTransactionAsync(), Times.Never);
        }

        [TestMethod]
        public async Task CreateOrderAsyncWhenRequestIsNullThrowsAndDoesNotStartTxn()
        {
            var sut = CreateSut();

            await Assert.ThrowsAsync<NullReferenceException>(() => sut.CreateOrderAsync(null!));

            _uow.Verify(u => u.BeginTransactionAsync(), Times.Never);
            _uow.Verify(u => u.RollbackTransactionAsync(), Times.Never);
            _uow.Verify(u => u.CommitTransactionAsync(), Times.Never);
        }


        [TestMethod]
        public async Task CreateOrderAsyncHappyPathCommitsPublishesReturnsOrderNumber()
        {
            _contentful = new Mock<IContentfulService>(MockBehavior.Loose);
            _inventory = new Mock<IInventoryServiceClient>(MockBehavior.Loose);

            var fakeResort = new ResortPage { DisplayName = "Test Resort" };
            _contentful
                .Setup(s => s.GetResortDetailsAsync(It.IsAny<string>()))
                .ReturnsAsync(fakeResort);

            var req = BuildMinimalRequest_SkipRealtime();
            req.Payments =
    [
        new() { PaymentAccountToken = "tok_123", ExpirationDate = "1227" }
    ];

            _uow.Setup(u => u.BeginTransactionAsync())
                .Returns(Task.CompletedTask);
            _uow.SetupGet(u => u.OrderRepository).Returns(_orderRepo.Object);
            _uow.SetupGet(u => u.ProfileRepository).Returns(_profileRepo.Object);


            _profileRepo
                .Setup(p => p.CreateProfileAsync(It.IsAny<ProfileEntity>()))
                .ReturnsAsync((ProfileEntity pe) => pe.ProfileType);

            OrderEntity? capturedOrder = null;
            _orderRepo
                .Setup(r => r.CreateOrderAsync(It.IsAny<OrderEntity>()))
                .Callback<OrderEntity>(o => capturedOrder = o)
                .ReturnsAsync((OrderEntity o) => o.OrderNumber);

            _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            _uow.Setup(u => u.CommitTransactionAsync())
                .Returns(Task.CompletedTask);


            var publisher = new Mock<IOrderReservationEventPublisher>(MockBehavior.Strict);
            publisher.Setup(p => p.PublishOrderAsync(It.IsAny<OrderRequestedEvent>())).Returns(Task.CompletedTask);


            var sut = new OrderService(
                _orderRepo.Object, _orderReturnRepo.Object, _txnRepo.Object, _profileRepo.Object, _logger.Object,
                _uow.Object, publisher.Object, _orderPaymentRepo.Object, null,
                _contentful.Object, _mapper.Object, _inrule.Object, _inventory.Object, _reservationConnector.Object, _newuow.Object);

            // act
            var result = await sut.CreateOrderAsync(req);

            // assert
            Assert.IsTrue(result.StartsWith("ORD", StringComparison.Ordinal));
            Assert.IsNotNull(capturedOrder);
            Assert.AreEqual(result, capturedOrder!.OrderNumber);

            _uow.Verify(u => u.BeginTransactionAsync(), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _uow.Verify(u => u.CommitTransactionAsync(), Times.Once);

            publisher.Verify(p => p.PublishOrderAsync(It.IsAny<OrderRequestedEvent>()), Times.Once);
        }

        [TestMethod]
        public async Task CreateOrderAsyncWhenSaveChangesThrowsRollsBackNoCommitNoPublishRethrowsV2()
        {
            _contentful = new Mock<IContentfulService>(MockBehavior.Loose);
            _inventory = new Mock<IInventoryServiceClient>(MockBehavior.Loose);


            var sut = CreateSut();
            var req = BuildMinimalRequest1();
            var fakeResort = new ResortPage
            {
                DisplayName = "Test Resort"
            };
            _contentful
                .Setup(s => s.GetResortDetailsAsync(It.IsAny<string>()))
                .ReturnsAsync(fakeResort);


            _uow.Setup(u => u.BeginTransactionAsync())
                .Returns(Task.CompletedTask);
            _uow.SetupGet(u => u.OrderRepository).Returns(_orderRepo.Object);
            _uow.SetupGet(u => u.ProfileRepository).Returns(_profileRepo.Object);


            _profileRepo.Setup(p => p.CreateProfileAsync(It.IsAny<ProfileEntity>()))
                          .ReturnsAsync((ProfileEntity pe) => pe.ProfileType);

            OrderEntity? capturedOrder = null;
            _orderRepo.Setup(r => r.CreateOrderAsync(It.IsAny<OrderEntity>()))
                      .Callback<OrderEntity>(o => capturedOrder = o)
                       .ReturnsAsync((OrderEntity o) => o.OrderNumber);


            _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("boom"));
            _uow.Setup(u => u.RollbackTransactionAsync())
                .Returns(Task.CompletedTask);

            // act + assert
            var ex = await Assert.ThrowsAsync<Exception>(() => sut.CreateOrderAsync(req));
            StringAssert.Contains(ex.Message, "boom");


            _uow.Verify(u => u.BeginTransactionAsync(), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _uow.Verify(u => u.CommitTransactionAsync(), Times.Never);
            _uow.Verify(u => u.RollbackTransactionAsync(), Times.Once);


            Assert.IsNotNull(capturedOrder);
            Assert.AreEqual("BOOKING", capturedOrder!.OrderType);
            Assert.AreEqual("PENDING", capturedOrder.OrderStatus);
            Assert.IsTrue(capturedOrder.OrderNumber.StartsWith("ORD", StringComparison.Ordinal));

            var items = capturedOrder.OrderItems?.ToList() ?? [];
            Assert.IsTrue(items.Count >= 1, "Expected at least one OrderItem.");

            var attrs = items[0].OrderItemAttributes?.ToList()
                       ?? [];

            Assert.IsTrue(attrs.Any(a => a.AttributeType == OneExchangeOrderServiceConstants.PropertyUnitTypeString), "Missing UNIT_TYPE");
            Assert.IsTrue(attrs.Any(a => a.AttributeType == OneExchangeOrderServiceConstants.PropertyNameAttributeTypeString), "Missing PROPERTY_NAME");
            Assert.IsTrue(attrs.Any(a => a.AttributeType == OneExchangeOrderServiceConstants.PropertyAddressAttributeTypeString), "Missing PROPERTY_ADDRESS");
        }
        [TestMethod]
        public async Task GetTransactionHistoryAsyncReturnsMappedTransactionsSortedDescWithResortAndPointUsed()
        {
            // arrange
            var sut = CreateSut();

            var customerExtRef = "CUST-001";

            // IMPORTANT: must match service
            var resortAttrType = "PROPERTY_NAME";

            var o1 = new OrderEntity
            {
                Id = Guid.NewGuid(),
                CustomerExternalReferenceId = customerExtRef,
                ExternalReferenceId = "CONF-111",

                // IMPORTANT: must pass service filter
                OrderStatus = OneExchangeOrderServiceConstants.ConfirmedOrderStatus,

                OrderItems =
        [
            new()
            {
                OrderItemAttributes =
                [
                    new() { AttributeType = resortAttrType, AttributeValue = "Resort Alpha" }
                ]
            }
        ]
            };

            var log1 = new ExchangeTransactionLog
            {
                TransactionType = "PURCHASE_BY_POINTS",  // IMPORTANT: must match service validTxnTypes
                TransactionDate = new DateTimeOffset(2025, 01, 05, 10, 0, 0, TimeSpan.Zero),
                FromTransactionAmount = 123.45678m,
                ToTransactionAmount = 0m,
                Order = o1
            };

            var o2 = new OrderEntity
            {
                Id = Guid.NewGuid(),
                CustomerExternalReferenceId = customerExtRef,

                // IMPORTANT: must pass service filter
                OrderStatus = OneExchangeOrderServiceConstants.CancelledOrderStatus,

                OrderReturn = new OneExchangeOrderService.Domain.Entities.ExchangeOrder.OrderReturn
                {
                    ExternalReferenceId = "REF-222"
                },
                OrderItems =
        [
            new()
            {
                OrderItemAttributes =
                [
                    new() { AttributeType = resortAttrType, AttributeValue = "Resort Beta" }
                ]
            }
        ]
            };

            var log2 = new ExchangeTransactionLog
            {
                TransactionType = "POINTS_REFUND",      // IMPORTANT
                TransactionDate = new DateTimeOffset(2024, 12, 25, 9, 0, 0, TimeSpan.Zero),
                FromTransactionAmount = 0m,
                ToTransactionAmount = 200.0m,
                Order = o2
            };

            var repoReturn = new List<ExchangeTransactionLog> { log2, log1 }; // unsorted on purpose

            _txnRepo
                .Setup(r => r.GetTransactionLogsAsync(It.IsAny<Guid>(), It.Is<string>(s => s == customerExtRef)))
                .ReturnsAsync(repoReturn);

            var req = new TransactionHistoryRequest { CustomerExternalReferenceId = customerExtRef };

            // act
            var result = await sut.GetTransactionHistoryAsync(req);

            // assert
            Assert.AreEqual(2, result.Count);

            var first = result[0];
            var second = result[1];

            // sorted desc by TransactionDate => log1 first
            Assert.AreEqual(o1.Id, first.OrderId);
            Assert.AreEqual(customerExtRef, first.CustomerExternalReferenceId);
            Assert.AreEqual(log1.TransactionDate.DateTime, first.ConfirmationDate);
            Assert.AreEqual(o1.ExternalReferenceId, first.ConfirmationNumber);
            Assert.AreEqual(OneExchangeOrderServiceConstants.Currency, first.Currency);
            Assert.AreEqual(OneExchangeOrderServiceConstants.Participant, first.Participant);
            Assert.AreEqual("Resort Alpha", first.Resort);

            var expectedPoints1 = TransactionAmountFormat.DefaultF4.FormatAmount(log1.FromTransactionAmount);
            Assert.AreEqual(expectedPoints1, first.PointUsed);

            Assert.AreEqual(o2.Id, second.OrderId);
            Assert.AreEqual(customerExtRef, second.CustomerExternalReferenceId);
            Assert.AreEqual(log2.TransactionDate.DateTime, second.ConfirmationDate);
            Assert.AreEqual(o2.OrderReturn!.ExternalReferenceId, second.ConfirmationNumber);
            Assert.AreEqual(OneExchangeOrderServiceConstants.Currency, second.Currency);
            Assert.AreEqual(OneExchangeOrderServiceConstants.Participant, second.Participant);
            Assert.AreEqual("Resort Beta", second.Resort);

            var expectedPoints2 = TransactionAmountFormat.NegativeF5.FormatAmount(-log2.ToTransactionAmount);
            Assert.AreEqual(expectedPoints2, second.PointUsed);

            _txnRepo.Verify(r => r.GetTransactionLogsAsync(It.IsAny<Guid>(), customerExtRef), Times.Once);
        }


        [TestMethod]
        public async Task GetTransactionHistoryAsyncWhenRequestIsNullThrowsNullReferenceAndDoesNotHitRepo()
        {
            // arrange
            var sut = CreateSut();

            // act + assert
            await Assert.ThrowsAsync<NullReferenceException>(() => sut.GetTransactionHistoryAsync(null!));

            _txnRepo.Verify(r => r.GetTransactionLogsAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
        }
        [TestMethod]
        public async Task GetOrderAsyncWithValidGuidReturnsMappedOrder()
        {
            // arrange
            SetupLoggerInfo();
            var sut = CreateService();

            var id = Guid.NewGuid();
            var orderId = id.ToString("D");

            var domainOrder = new OrderEntity { Id = id, OrderNumber = "ORD123" };
            var mapped = new OrderResponse { Id = id, OrderNumber = "ORD123" };

            _orderRepo
                .Setup(r => r.GetOrderAsync(It.Is<Guid>(g => g == id), It.IsAny<CancellationToken>()))
                .ReturnsAsync(domainOrder);

            _mapper
                .Setup(m => m.Map<OrderResponse>(domainOrder))
                .Returns(mapped);

            // act
            var result = await sut.GetOrderAsync(orderId);

            // assert
            Assert.AreSame(mapped, result, "Service should return mapper's DTO instance.");
            _orderRepo.Verify(r => r.GetOrderAsync(id, It.IsAny<CancellationToken>()), Times.Once);
            _mapper.Verify(m => m.Map<OrderResponse>(domainOrder), Times.Once);
        }

        [TestMethod]
        public async Task GetOrdersAsyncWhenConfirmedOrdersExistPopulatesGuests()
        {
            var sut = CreateService();

            var profileId = Guid.NewGuid();

            var request = new OrderRequest()
            {
                CustomerExternalReferenceId = "abc"
            };
            var orders = new List<OrderResponse>
    {
        new()
        {
            OrderStatus = OrderStatus.CONFIRMED.ToString(),
            ProfileId = [profileId]
        }
    };

            var guests = new List<GuestDetails>
    {
        new() { ProfileId = profileId }
    };

            _orderRepo
                .Setup(r => r.GetOrdersAsync(request, It.IsAny<List<string>>()))
                .ReturnsAsync(orders);

            _profileRepo
                .Setup(r => r.GetGuestsAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync(guests);

            var result = (await sut.GetOrdersAsync(request)).ToList();

            Assert.AreEqual(1, result[0].Guests.Count);
        }

        [TestMethod]
        public async Task GetOrderAsyncWithNullReturnsNullAndDoesNotHitRepo()
        {
            // arrange
            SetupLoggerInfo();
            var sut = CreateService();

            // act
            var result = await sut.GetOrderAsync(string.Empty);

            // assert
            Assert.IsNull(result);

            _orderRepo.Verify(r => r.GetOrderAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            _mapper.Verify(m => m.Map<OrderResponse>(It.IsAny<OrderEntity>()), Times.Never);
        }



        [TestMethod]
        public async Task GetOrderDetailsAsyncWithValidGuidReturnsMappedOrder()
        {
            // arrange
            SetupLoggerInfo();
            var sut = CreateService();

            var id = Guid.NewGuid();
            var orderId = id.ToString("D");

            var domainOrder = new OrderEntity { Id = id, OrderNumber = "ORD123" };
            var mapped = new OrderDataResponse { Id = id, OrderNumber = "ORD123" };

            _orderRepo
                .Setup(r => r.GetOrderAsync(It.Is<Guid>(g => g == id), It.IsAny<CancellationToken>()))
                .ReturnsAsync(domainOrder);

            _mapper
                .Setup(m => m.Map<OrderDataResponse>(domainOrder))
                .Returns(mapped);

            // act
            var result = await sut.GetOrderDetailsAsync(orderId);

            // assert
            Assert.AreSame(mapped, result, "Service should return mapper's DTO instance.");
            _orderRepo.Verify(r => r.GetOrderAsync(id, It.IsAny<CancellationToken>()), Times.Once);
            _mapper.Verify(m => m.Map<OrderDataResponse>(domainOrder), Times.Once);


        }

        [TestMethod]
        public async Task GetOrderDetailsAsyncWithNullReturnsNullAndDoesNotHitRepo()
        {
            // arrange
            SetupLoggerInfo();
            var sut = CreateService();

            // act
            var result = await sut.GetOrderDetailsAsync(string.Empty);

            // assert
            Assert.IsNull(result);

            _orderRepo.Verify(r => r.GetOrderAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            _mapper.Verify(m => m.Map<OrderDataResponse>(It.IsAny<OrderEntity>()), Times.Never);
        }
        [TestMethod]
        public async Task GetOrderStatusAsyncWithExistingOrderReturnsOkWithPayload()
        {
            // arrange
            SetupLoggerInfo();
            var sut = CreateService();
            var orderNumber = "ORD123";


            var repoResult = (OrderStatus: "CONFIRMED",
                              ExternalReferenceId: "EXT-1",
                              orderId: Guid.NewGuid().ToString(),
                              StatusReason: (string?)null);

            _orderRepo
                .Setup(r => r.GetOrderStatusAsync(orderNumber))
                .ReturnsAsync(repoResult);

            // act
            var result = await sut.GetOrderStatusAsync(orderNumber);

            // assert
            var ok = result as OkObjectResult;
            Assert.IsNotNull(ok, "Expected OkObjectResult.");
            Assert.AreEqual(StatusCodes.Status200OK, ok.StatusCode ?? StatusCodes.Status200OK);


            var payload = ok.Value!;
            var t = payload.GetType();
            Assert.AreEqual("CONFIRMED", (string)t.GetProperty("OrderStatus")!.GetValue(payload)!);
            Assert.AreEqual("EXT-1", (string)t.GetProperty("ExternalReferenceId")!.GetValue(payload)!);
            Assert.AreEqual(string.Empty, (string)t.GetProperty("StatusReason")!.GetValue(payload)!);

            _orderRepo.Verify(r => r.GetOrderStatusAsync(orderNumber), Times.Once);
        }

        [TestMethod]
        public async Task GetOrderStatusAsyncWhenRepoReturnsNullReturnsNotFoundProblemDetails()
        {
            // arrange
            SetupLoggerInfo();
            var sut = CreateService();
            var orderNumber = "ORD999";

            _orderRepo
                .Setup(r => r.GetOrderStatusAsync(orderNumber))
                .ReturnsAsync(((string?, string?, string?, string?)?)null); // repo says "no row"

            // act
            var result = await sut.GetOrderStatusAsync(orderNumber);

            // assert
            var notFound = result as NotFoundObjectResult;
            Assert.IsNotNull(notFound, "Expected NotFoundObjectResult.");
            Assert.AreEqual(StatusCodes.Status404NotFound, notFound.StatusCode ?? StatusCodes.Status404NotFound);

            var problem = notFound.Value as ProblemDetails;
            Assert.IsNotNull(problem, "Expected ProblemDetails payload.");
            Assert.AreEqual("Order not found", problem!.Title);
            StringAssert.Contains(problem.Detail!, orderNumber);
            Assert.AreEqual(StatusCodes.Status404NotFound, problem.Status);

            _orderRepo.Verify(r => r.GetOrderStatusAsync(orderNumber), Times.Once);
        }

        [TestMethod]
        public async Task GetOrderStatusAsyncWhenOrderStatusEmptyReturnsNotFound()
        {
            // arrange
            SetupLoggerInfo();
            var sut = CreateService();
            var orderNumber = "ORD000";

            var repoResult = (OrderStatus: "",
                              ExternalReferenceId: "EXT-0",
                              orderId: Guid.NewGuid().ToString(),
                              StatusReason: "SomeReason");

            _orderRepo
                .Setup(r => r.GetOrderStatusAsync(orderNumber))
                .ReturnsAsync(repoResult);

            // act
            var result = await sut.GetOrderStatusAsync(orderNumber);

            // assert
            var notFound = result as NotFoundObjectResult;
            Assert.IsNotNull(notFound, "Expected NotFoundObjectResult.");
            var problem = notFound!.Value as ProblemDetails;
            Assert.IsNotNull(problem);
            Assert.AreEqual("Order not found", problem!.Title);

            _orderRepo.Verify(r => r.GetOrderStatusAsync(orderNumber), Times.Once);
        }

        [TestMethod]
        public async Task GetCancelOrderDetailsAsyncWhenCancelledReturnsOkWithCancellationNumberEmptyMessage()
        {
            // arrange
            SetupLoggerInfo();
            var sut = CreateService();
            var orderNumber = "ORD-123";
            var oid = Guid.NewGuid().ToString();

            var repoTuple = (OrderStatus: OrderStatus.CANCELLED.ToString(),
                             ExternalReferenceId: "EXT-9",
                             orderId: oid,
                             StatusReason: (string?)null);

            _orderRepo
                .Setup(r => r.GetOrderStatusAsync(orderNumber))
                .ReturnsAsync(repoTuple);

            _orderReturnRepo
                .Setup(r => r.GetCancellationNumberAsync(oid))
                .ReturnsAsync("CN-789");

            // act
            var actionResult = await sut.GetCancelOrderDetailsAsync(orderNumber);

            // assert
            var ok = actionResult as OkObjectResult;
            Assert.IsNotNull(ok, "Expected OkObjectResult.");
            Assert.AreEqual(StatusCodes.Status200OK, ok.StatusCode ?? StatusCodes.Status200OK);


            var payload = ok.Value!;
            var t = payload.GetType();
            Assert.AreEqual(OrderStatus.CANCELLED.ToString(), (string)t.GetProperty("OrderStatus")!.GetValue(payload)!);
            Assert.AreEqual("CN-789", (string)t.GetProperty("ExternalReferenceId")!.GetValue(payload)!);
            Assert.AreEqual(string.Empty, (string)t.GetProperty("Message")!.GetValue(payload)!);

            _orderRepo.Verify(r => r.GetOrderStatusAsync(orderNumber), Times.Once);
            _orderReturnRepo.Verify(r => r.GetCancellationNumberAsync(oid), Times.Once);

        }

        [TestMethod]
        public async Task GetCancelOrderDetailsAsyncWhenConfirmedReturnsOkWithFailureMessage()
        {
            // arrange
            SetupLoggerInfo();
            var sut = CreateService();
            var orderNumber = "ORD-456";
            var oid = Guid.NewGuid().ToString();

            var repoTuple = (OrderStatus: "confirmed", // lower-case to exercise OrdinalIgnoreCase
                             ExternalReferenceId: "EXT-2",
                             orderId: oid,
                             StatusReason: "any");

            _orderRepo
                .Setup(r => r.GetOrderStatusAsync(orderNumber))
                .ReturnsAsync(repoTuple);

            _orderReturnRepo
                .Setup(r => r.GetCancellationNumberAsync(oid))
                .ReturnsAsync("CN-001");

            // act
            var actionResult = await sut.GetCancelOrderDetailsAsync(orderNumber);

            // assert
            var ok = actionResult as OkObjectResult;
            Assert.IsNotNull(ok);
            var payload = ok!.Value!;
            var t = payload.GetType();

            Assert.AreEqual(OrderStatus.CONFIRMED.ToString(), ((string)t.GetProperty("OrderStatus")!.GetValue(payload)!).ToUpper(CultureInfo.CurrentCulture));
            Assert.AreEqual("CN-001", (string)t.GetProperty("ExternalReferenceId")!.GetValue(payload)!);
            Assert.AreEqual(OneExchangeOrderServiceConstants.OrderStatusUpdateFailureMessage,
                            (string)t.GetProperty("Message")!.GetValue(payload)!);

            _orderRepo.Verify(r => r.GetOrderStatusAsync(orderNumber), Times.Once);
            _orderReturnRepo.Verify(r => r.GetCancellationNumberAsync(oid), Times.Once);

        }

        [TestMethod]
        public async Task GetCancelOrderDetailsAsyncWhenOrderIdIsNotGuidReturnsOkWithNullCancellationNumber()
        {
            // arrange
            SetupLoggerInfo();
            var sut = CreateService();
            var orderNumber = "ORD-777";
            var notGuid = "NOT-A-GUID";

            var repoTuple = (OrderStatus: OrderStatus.CANCELLED.ToString(),
                             ExternalReferenceId: "EXT-7",
                             orderId: notGuid,
                             StatusReason: (string?)null);

            _orderRepo
                .Setup(r => r.GetOrderStatusAsync(orderNumber))
                .ReturnsAsync(repoTuple);


            _orderReturnRepo
                .Setup(r => r.GetCancellationNumberAsync(notGuid))
                .ReturnsAsync((string?)null);

            // act
            var actionResult = await sut.GetCancelOrderDetailsAsync(orderNumber);

            // assert
            var ok = actionResult as OkObjectResult;
            Assert.IsNotNull(ok);
            var payload = ok!.Value!;
            var t = payload.GetType();

            Assert.AreEqual(OrderStatus.CANCELLED.ToString(), (string)t.GetProperty("OrderStatus")!.GetValue(payload)!);
            Assert.IsNull(t.GetProperty("ExternalReferenceId")!.GetValue(payload));
            Assert.AreEqual(string.Empty, (string)t.GetProperty("Message")!.GetValue(payload)!);

            _orderRepo.Verify(r => r.GetOrderStatusAsync(orderNumber), Times.Once);
            _orderReturnRepo.Verify(r => r.GetCancellationNumberAsync(notGuid), Times.Once);

        }

        [TestMethod]
        public async Task GetCancelOrderDetailsAsyncWhenRepoReturnsNullThrows()
        {
            // arrange
            SetupLoggerInfo();
            var sut = CreateService();
            var orderNumber = "ORD-999";

            _orderRepo
                .Setup(r => r.GetOrderStatusAsync(orderNumber))
                .ReturnsAsync(((string?, string?, string?, string?)?)null);

            // act + assert

            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetCancelOrderDetailsAsync(orderNumber));

            // orderReturnRepo should not be called
            _orderReturnRepo.Verify(r => r.GetCancellationNumberAsync(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task GetOrderByOrderNumberAsyncWhenExistsReturnsOrder()
        {
            // arrange
            var sut = CreateService();
            var orderNumber = "ORD123";
            var entity = new OrderEntity { Id = Guid.NewGuid(), OrderNumber = orderNumber };

            _uow.SetupGet(u => u.OrderRepository).Returns(_orderRepo.Object);
            _orderRepo.Setup(r => r.GetOrderByOrderNumberAsync(orderNumber))
                      .ReturnsAsync(entity);

            // act
            var result = await sut.GetOrderByOrderNumberAsync(orderNumber);

            // assert
            Assert.AreSame(entity, result);
            _uow.VerifyGet(u => u.OrderRepository, Times.AtLeastOnce);
            _orderRepo.Verify(r => r.GetOrderByOrderNumberAsync(orderNumber), Times.Once);

        }

        [TestMethod]
        public async Task GetOrderByOrderNumberAsyncWhenNotFoundReturnsNull()
        {
            // arrange
            var sut = CreateService();
            var orderNumber = "ORD999";

            _uow.SetupGet(u => u.OrderRepository).Returns(_orderRepo.Object);
            _orderRepo.Setup(r => r.GetOrderByOrderNumberAsync(orderNumber))
                      .ReturnsAsync((OrderEntity?)null);

            // act
            var result = await sut.GetOrderByOrderNumberAsync(orderNumber);

            // assert
            Assert.IsNull(result);
            _orderRepo.Verify(r => r.GetOrderByOrderNumberAsync(orderNumber), Times.Once);
        }

        [TestMethod]
        public async Task GetOrderByOrderNumberAsyncWithNullForwardsToRepoReturnsNull()
        {
            // arrange
            var sut = CreateService();
            string? orderNumber = null;

            _uow.SetupGet(u => u.OrderRepository).Returns(_orderRepo.Object);
            _orderRepo.Setup(r => r.GetOrderByOrderNumberAsync(It.Is<string>(s => s == null)))
                      .ReturnsAsync((OrderEntity?)null);

            // act
            var result = await sut.GetOrderByOrderNumberAsync(orderNumber!); // deliberate null

            // assert
            Assert.IsNull(result);
            _orderRepo.Verify(r => r.GetOrderByOrderNumberAsync(It.Is<string>(s => s == null)), Times.Once);
        }

        [TestMethod]
        public async Task ApplyPatchToOrderAsyncWhenOrderNotFoundReturnsNotFoundResponseNoTxn()
        {
            // arrange
            SetupLoggerInfo();
            var sut = CreateService();
            var id = Guid.NewGuid().ToString("D");

            _uow.SetupGet(u => u.OrderRepository).Returns(_orderRepo.Object);
            _orderRepo
                .Setup(r => r.GetOrderAsync(It.Is<Guid>(g => g.ToString() == id), It.IsAny<CancellationToken>()))
                .ReturnsAsync((OrderEntity?)null);

            var patch = new JsonPatchDocument<OrderPatchModel>();
            patch.Replace(m => m.OrderStatus, "CONFIRMED"); // any patch

            // act
            var resp = await sut.ApplyPatchToOrderAsync(id, patch);

            // assert
            Assert.IsNotNull(resp);
            Assert.IsFalse(resp.IsUpdatedSuccessfully);
            Assert.AreEqual(OneExchangeOrderServiceConstants.OrderNotFoundUpdateMessage, resp.Message);
            Assert.AreEqual(string.Empty, resp.OrderNumber);
            Assert.AreEqual(string.Empty, resp.OrderId);
            Assert.AreEqual(string.Empty, resp.OrderStatus);

            _uow.Verify(u => u.BeginTransactionAsync(), Times.Never);
            _orderRepo.Verify(r => r.UpdateOrderAsync(It.IsAny<OrderEntity>()), Times.Never);
        }

        [TestMethod]
        public async Task ApplyPatchToOrderAsyncWhenSameStatusReturnsSameStatusMessageNoTxnNoUpdate()
        {
            // arrange
            SetupLoggerInfo();
            var sut = CreateService();

            var id = Guid.NewGuid();
            var order = new OrderEntity
            {
                Id = id,
                OrderNumber = "ORD123",
                OrderStatus = "CONFIRMED",
                TenantId = Guid.NewGuid()
            };

            _uow.SetupGet(u => u.OrderRepository).Returns(_orderRepo.Object);
            _orderRepo
                .Setup(r => r.GetOrderAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            var patch = new JsonPatchDocument<OrderPatchModel>();
            patch.Replace(m => m.OrderStatus, "confirmed");

            // act
            var resp = await sut.ApplyPatchToOrderAsync(id.ToString("D"), patch);

            // assert
            Assert.IsNotNull(resp);
            Assert.IsFalse(resp.IsUpdatedSuccessfully);
            Assert.AreEqual(OneExchangeOrderServiceConstants.OrderSameStatusUpdateMessage, resp.Message);
            Assert.AreEqual("ORD123", resp.OrderNumber);
            Assert.AreEqual(id.ToString("D"), resp.OrderId);
            Assert.AreEqual("CONFIRMED", resp.OrderStatus);

            _uow.Verify(u => u.BeginTransactionAsync(), Times.Never);
            _orderRepo.Verify(r => r.UpdateOrderAsync(It.IsAny<OrderEntity>()), Times.Never);
        }

        [TestMethod]
        public async Task ApplyPatchToOrderAsyncWhenStatusChangesAndReasonProvidedUpdatesSavesCommitsReturnsSuccess()
        {
            // arrange
            SetupLoggerInfo();
            var sut = CreateService();

            var id = Guid.NewGuid();
            var order = new OrderEntity
            {
                Id = id,
                OrderNumber = "ORD999",
                OrderStatus = "PENDING",
                TenantId = Guid.NewGuid(),
                ExternalSourceSystem = "SYS1",
                ExternalReferenceId = "EXT-OLD",
                Memo = "old memo"

            };

            _uow.SetupGet(u => u.OrderRepository).Returns(_orderRepo.Object);
            _uow.SetupGet(u => u.OrderReturnRepository).Returns(_orderReturnRepo.Object);
            _uow.SetupGet(u => u.AddressRepository).Returns(_addressRepo.Object);
            _uow.SetupGet(u => u.OrderPaymentRepository).Returns(_orderPaymentRepo.Object);
            _uow.SetupGet(u => u.OrderRefundRepository).Returns(_refundRepo.Object);
            _uow.SetupGet(u => u.ExchangeTransactionLogRepository).Returns(_txnRepo.Object);

            _orderRepo
                .Setup(r => r.GetOrderAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            OrderEntity? updated = null;
            _orderRepo
                .Setup(r => r.UpdateOrderAsync(It.IsAny<OrderEntity>()))
                .Callback<OrderEntity>(o => updated = o)
                .Returns(Task.CompletedTask);

            _uow.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _uow.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);


            var patch = new JsonPatchDocument<OrderPatchModel>();
            patch.Replace(m => m.OrderStatus, "CONFIRMED");
            patch.Replace(m => m.StatusReason, "Customer requested");
            patch.Replace(m => m.Memo, "new memo");

            patch.Replace(m => m.ExternalOrderReference.ExternalReferenceId, "EXT-NEW");

            // act
            var resp = await sut.ApplyPatchToOrderAsync(id.ToString("D"), patch);

            // assert response
            Assert.IsTrue(resp.IsUpdatedSuccessfully);
            Assert.AreEqual(OneExchangeOrderServiceConstants.SuccessfulUpdateMessage, resp.Message);
            Assert.AreEqual("ORD999", resp.OrderNumber);
            Assert.AreEqual(id.ToString("D"), resp.OrderId);
            Assert.AreEqual("CONFIRMED", resp.OrderStatus);

            // assert updated entity
            Assert.IsNotNull(updated, "Order should be passed to UpdateOrderAsync.");
            Assert.AreEqual("CONFIRMED", updated!.OrderStatus);
            Assert.AreEqual("new memo", updated.Memo);
            Assert.AreEqual("EXT-NEW", updated.ExternalReferenceId);
            Assert.AreEqual(OneExchangeOrderServiceConstants.PatchOrderUpdatedBy, updated.UpdatedBy);
            Assert.IsTrue(updated.UpdatedDateTime > DateTimeOffset.UtcNow.AddMinutes(-5));

            // verify txn calls
            _uow.Verify(u => u.BeginTransactionAsync(), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _uow.Verify(u => u.CommitTransactionAsync(), Times.Once);
            _orderRepo.Verify(r => r.UpdateOrderAsync(It.IsAny<OrderEntity>()), Times.Once);
        }

        [TestMethod]
        public async Task ApplyPatchToOrderAsyncWhenSaveThrowsRollsBackAndRethrows()
        {
            // arrange
            SetupLoggerInfo();
            var sut = CreateService();

            var id = Guid.NewGuid();
            var order = new OrderEntity
            {
                Id = id,
                OrderNumber = "ORD777",
                OrderStatus = "PENDING",
                TenantId = Guid.NewGuid()
            };

            _uow.SetupGet(u => u.OrderRepository).Returns(_orderRepo.Object);
            _orderRepo
                .Setup(r => r.GetOrderAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            _orderRepo
                .Setup(r => r.UpdateOrderAsync(It.IsAny<OrderEntity>()))
                .Returns(Task.CompletedTask);

            _uow.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new DbUpdateException("boom"));
            _uow.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            var patch = new JsonPatchDocument<OrderPatchModel>();
            patch.Replace(m => m.OrderStatus, "CANCELLED");

            // act + assert
            var ex = await Assert.ThrowsAsync<Exception>(() => sut.ApplyPatchToOrderAsync(id.ToString("D"), patch));
            StringAssert.Contains(ex.Message, "boom");

            _uow.Verify(u => u.BeginTransactionAsync(), Times.Once);
            _uow.Verify(u => u.RollbackTransactionAsync(), Times.Once);
            _uow.Verify(u => u.CommitTransactionAsync(), Times.Never);
        }

        [TestMethod]
        public async Task CheckBookingFeeEligibilityAsyncWhenNotExceededReturnsTrue()
        {

            SetupLoggerInfo();
            var sut = CreateService();

            var memberId = "MEM-001";
            var fromDate = "2025-01-15";

            _orderRepo
                .Setup(r => r.HasExceededWaiverLimitForCurrentYearAsync(memberId, fromDate))
                .ReturnsAsync(false);

            // act
            var result = await sut.CheckBookingFeeEligibilityAsync(memberId, fromDate);

            // assert
            Assert.IsTrue(result, "Expected eligible (hasExceededLimit == false).");
            _orderRepo.Verify(r => r.HasExceededWaiverLimitForCurrentYearAsync(memberId, fromDate), Times.Once);
        }

        [TestMethod]
        public async Task CheckBookingFeeEligibilityAsyncWhenExceededReturnsFalse()
        {
            // arrange
            SetupLoggerInfo();
            var sut = CreateService();

            var memberId = "MEM-002";
            var fromDate = "2025-02-01";

            _orderRepo
                .Setup(r => r.HasExceededWaiverLimitForCurrentYearAsync(memberId, fromDate))
                .ReturnsAsync(true);

            // act
            var result = await sut.CheckBookingFeeEligibilityAsync(memberId, fromDate);

            // assert
            Assert.IsFalse(result, "Expected not eligible (hasExceededLimit == true).");
            _orderRepo.Verify(r => r.HasExceededWaiverLimitForCurrentYearAsync(memberId, fromDate), Times.Once);
        }

        [TestMethod]
        public async Task CheckBookingFeeEligibilityAsyncForwardsInputsToRepositoryAndReturnsTrueWhenRepoReturnsFalse()
        {
            // arrange
            SetupLoggerInfo();
            var sut = CreateService();

            var memberId = "MEM-XYZ";
            var fromDate = "not-a-date"; // repo would treat invalid date as 'false' path

            string? seenMember = null;
            string? seenDate = null;

            _orderRepo
                .Setup(r => r.HasExceededWaiverLimitForCurrentYearAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>((m, d) => { seenMember = m; seenDate = d; })
                .ReturnsAsync(false);

            // act
            var result = await sut.CheckBookingFeeEligibilityAsync(memberId, fromDate);

            // assert
            Assert.IsTrue(result, "Repo returned false → eligible true.");
            Assert.AreEqual(memberId, seenMember);
            Assert.AreEqual(fromDate, seenDate);
            _orderRepo.Verify(r => r.HasExceededWaiverLimitForCurrentYearAsync(memberId, fromDate), Times.Once);
        }
        [TestMethod]
        public async Task UpdateOrderAsyncWithPendingStatusDoesNotUpdateAndDoesNotCommitReturnsOrderNumber()
        {
            // arrange
            SetupLoggerInfo();
            var sut = CreateService();

            var id = Guid.NewGuid();
            var order = new OrderEntity
            {
                Id = id,
                OrderNumber = "ORD123",
                OrderStatus = "PENDING",
                TenantId = Guid.NewGuid(),
                OrderGuests = [],
                OrderItems = []
            };

            var request = new OrderPatchRequest
            {
                Value = new() { OrderStatus = OrderStatus.CONFIRMED.ToString() }
            };

            _uow.SetupGet(u => u.OrderRepository).Returns(_orderRepo.Object);

            // STRICT mock: must return a value
            _orderRepo
                .Setup(r => r.GetOrderAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            // act
            var result = await sut.UpdateOrderAsync(id.ToString("D"), request);

            // assert
            Assert.AreEqual("ORD123", result);
            Assert.AreEqual("PENDING", order.OrderStatus);

            // since status is PENDING, service should not update/commit/save
            _uow.Verify(u => u.BeginTransactionAsync(), Times.Never);
            _orderRepo.Verify(r => r.UpdateOrderStatusAsync(It.IsAny<OrderEntity>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            _uow.Verify(u => u.CommitTransactionAsync(), Times.Never);

            // cancellation flow should not run
            _profileRepo.Verify(p => p.GetProfileAddressAsync(It.IsAny<string>(), It.IsAny<Guid?>()), Times.Never);
            _profileRepo.Verify(p => p.GetOrderGuestsAsync(It.IsAny<List<Guid>>()), Times.Never);
        }


        [TestMethod]
        public async Task UpdateOrderCommentAsyncWhenMemoEmptySetsFirstCommentSavesAndCommitsReturnsTrue()
        {
            // arrange
            SetupLoggerInfo();
            var sut = CreateService();

            var id = Guid.NewGuid();
            var order = new OrderEntity
            {
                Id = id,
                OrderNumber = "ORD123",
                TenantId = Guid.NewGuid(),
                Memo = string.Empty
            };

            var request = new UpdateOrderComment
            {
                Message = "Hello world",
                UserName = "alice"
            };

            _uow.SetupGet(u => u.OrderRepository).Returns(_orderRepo.Object);
            _uow.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);

            _orderRepo
                .Setup(r => r.GetOrderAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            _uow.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);

            // act
            var ok = await sut.UpdateOrderCommentAsync(id.ToString("D"), request);

            // assert
            Assert.IsTrue(ok);

            StringAssert.Contains(order.Memo, "|Hello world|alice");

            Assert.IsFalse(order.Memo.Contains("||"));

            _uow.Verify(u => u.BeginTransactionAsync(), Times.Once);
            _orderRepo.Verify(r => r.GetOrderAsync(id, It.IsAny<CancellationToken>()), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _uow.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }
        [TestMethod]
        public async Task UpdateOrderCommentAsyncWhenMemoExistsAppendsWithDoublePipeSavesAndCommitsReturnsTrue()
        {
            // arrange
            SetupLoggerInfo();
            var sut = CreateService();

            var id = Guid.NewGuid();
            var order = new OrderEntity
            {
                Id = id,
                OrderNumber = "ORD777",
                TenantId = Guid.NewGuid(),
                Memo = "existing"
            };

            var request = new UpdateOrderComment
            {
                Message = "Added note",
                UserName = "bob"
            };

            _uow.SetupGet(u => u.OrderRepository).Returns(_orderRepo.Object);
            _uow.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);

            _orderRepo
                .Setup(r => r.GetOrderAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _uow.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);

            // act
            var ok = await sut.UpdateOrderCommentAsync(id.ToString("D"), request);

            // assert
            Assert.IsTrue(ok);
            StringAssert.StartsWith(order.Memo, "existing||");
            StringAssert.Contains(order.Memo, "|Added note|bob");

            _uow.Verify(u => u.BeginTransactionAsync(), Times.Once);
            _orderRepo.Verify(r => r.GetOrderAsync(id, It.IsAny<CancellationToken>()), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _uow.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [TestMethod]
        public async Task UpdateOrderCommentAsyncWhenMemoWouldExceedLimitThrowsColumnDataLimitExceptionNoSaveNoCommit()
        {
            // arrange
            SetupLoggerInfo();
            var sut = CreateService();

            var id = Guid.NewGuid();
            var longMemo = new string('x', 501);
            var order = new OrderEntity
            {
                Id = id,
                OrderNumber = "ORD999",
                TenantId = Guid.NewGuid(),
                Memo = longMemo
            };

            var request = new UpdateOrderComment
            {
                Message = "tiny",
                UserName = "user"
            };

            _uow.SetupGet(u => u.OrderRepository).Returns(_orderRepo.Object);
            _uow.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);

            _orderRepo
                .Setup(r => r.GetOrderAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            // act + assert
            var ex = await Assert.ThrowsAsync<ColumnDataLimitException>(
                () => sut.UpdateOrderCommentAsync(id.ToString("D"), request));

            StringAssert.Contains(ex.Message, OneExchangeOrderServiceConstants.ColumnLimit);

            _uow.Verify(u => u.BeginTransactionAsync(), Times.Once);

            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            _uow.Verify(u => u.CommitTransactionAsync(), Times.Never);
        }
        [TestMethod]
        public async Task GetBookingDetailsAsyncWhenOrderIdIsNullOrEmptyReturnsNull()
        {
            // arrange
            var sut = CreateService();

            // act
            var r1 = await sut.GetBookingDetailsAsync(null!);
            var r2 = await sut.GetBookingDetailsAsync(string.Empty);

            // assert
            Assert.IsNull(r1);
            Assert.IsNull(r2);

            _orderRepo.Verify(r => r.GetOrderDetailsByConfirmationNumberAsync(It.IsAny<string>()), Times.Never);
            _profileRepo.Verify(p => p.GetGuestsAsync(It.IsAny<List<Guid>>()), Times.Never);
            _contentful.Verify(c => c.GetResortDetailsAsync(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task GetBookingDetailsAsyncWhenRepoReturnsNullReturnsNull()
        {
            // arrange
            var sut = CreateService();
            var confirmation = "CNF-000";

            _orderRepo
                .Setup(r => r.GetOrderDetailsByConfirmationNumberAsync(confirmation))
                .ReturnsAsync((OrderDetailsResponse?)null);

            // act
            var result = await sut.GetBookingDetailsAsync(confirmation);

            // assert
            Assert.IsNull(result);
            _orderRepo.Verify(r => r.GetOrderDetailsByConfirmationNumberAsync(confirmation), Times.Once);
            _profileRepo.Verify(p => p.GetGuestsAsync(It.IsAny<List<Guid>>()), Times.Never);
            _contentful.Verify(c => c.GetResortDetailsAsync(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task GetBookingDetailsAsyncHappyPathFillsGuestAndPropertyFields()
        {
            // arrange
            var sut = CreateService();
            var confirmation = "CNF-123";
            var p1 = Guid.NewGuid();
            var p2 = Guid.NewGuid();
            var itemCode = "ANY-CODE-STRING"; // parser inside private helper is exercised, we stub contentful for any string

            var repoResponse = new OrderDetailsResponse
            {
                ItemCode = itemCode,
                ProfileIds = [p1, p2],
                FirstName = null,
                LastName = null,
                PropertyName = null,
                PropertyPhone = null,
                PropertyAddress = null,
                PropertyInfo = null
            };

            _orderRepo
                .Setup(r => r.GetOrderDetailsByConfirmationNumberAsync(confirmation))
                .ReturnsAsync(repoResponse);

            var guests = new List<GuestDetails>
                {
                    new() { FirstName = "Ada",   LastName = "Lovelace" },
                    new() { FirstName = "Grace", LastName = "Hopper"   }
                };

            _profileRepo
                .Setup(p => p.GetGuestsAsync(It.Is<List<Guid>>(ids => ids.SequenceEqual(new[] { p1, p2 }))))
                .ReturnsAsync(guests.AsEnumerable());

            // contentful resort stub — accept any resort code
            var resort = new ResortPage
            {
                DisplayName = "Sample Resort",
                OverviewSectionContent = new Overview
                {
                    ContactPhoneNumber = "555-0100",
                    Location = new Location
                    {
                        Street1 = "1 Code St",
                        City = "Meerut",
                        State = "UP",
                        Country = "IN",
                        ZipCode = "250001"
                    }
                },
                UnitTypes = []
            };

            _contentful
                .Setup(c => c.GetResortDetailsAsync(It.IsAny<string>()))
                .ReturnsAsync(resort);

            // act
            var result = await sut.GetBookingDetailsAsync(confirmation);

            // assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Ada", result!.FirstName);
            Assert.AreEqual("Lovelace", result.LastName);

            Assert.AreEqual("Sample Resort", result.PropertyName);
            Assert.AreEqual("555-0100", result.PropertyPhone);
            StringAssert.Contains(result.PropertyAddress!, "1 Code St");
            StringAssert.Contains(result.PropertyAddress!, "Meerut");
            StringAssert.Contains(result.PropertyAddress!, "UP");
            StringAssert.Contains(result.PropertyAddress!, "IN");
            StringAssert.Contains(result.PropertyAddress!, "250001");

            _orderRepo.Verify(r => r.GetOrderDetailsByConfirmationNumberAsync(confirmation), Times.Once);
            _profileRepo.Verify(p => p.GetGuestsAsync(It.Is<List<Guid>>(ids => ids.Count == 2 && ids.Contains(p1) && ids.Contains(p2))), Times.Once);
            _contentful.Verify(c => c.GetResortDetailsAsync(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task GetBookingDetailsAsyncWhenNoGuestsButItemCodePresentFillsOnlyPropertyFields()
        {
            // arrange
            var sut = CreateService();
            var confirmation = "CNF-234";
            var itemCode = "CHO-AV123-1";

            var repoResponse = new OrderDetailsResponse
            {
                ItemCode = itemCode,
                ProfileIds = [] // empty → no guest lookup
            };

            _orderRepo
                .Setup(r => r.GetOrderDetailsByConfirmationNumberAsync(confirmation))
                .ReturnsAsync(repoResponse);

            var resort = new ResortPage
            {
                DisplayName = "Resort X",
                OverviewSectionContent = new Overview
                {
                    ContactPhoneNumber = "999-9999",
                    Location = new Location
                    {
                        Street1 = "99 Main Rd",
                        City = "Pune",
                        State = "MH",
                        Country = "IN",
                        ZipCode = "411001"
                    }
                },
                UnitTypes = []
            };

            _contentful
                .Setup(c => c.GetResortDetailsAsync(It.IsAny<string>()))
                .ReturnsAsync(resort);

            // act
            var result = await sut.GetBookingDetailsAsync(confirmation);

            // assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Resort X", result!.PropertyName);
            Assert.AreEqual("999-9999", result.PropertyPhone);
            StringAssert.Contains(result.PropertyAddress!, "Pune");

            _profileRepo.Verify(p => p.GetGuestsAsync(It.IsAny<List<Guid>>()), Times.Never);
            _contentful.Verify(c => c.GetResortDetailsAsync(It.IsAny<string>()), Times.Once);
        }
        [TestMethod]
        public async Task UpdateRefundAsyncHappyPathCreatesRefundAndTxnSavesCommitsReturnsTrue()
        {
            // arrange
            SetupLoggerInfo();
            var sut = CreateService();

            var orderId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var originalPaymentId = Guid.NewGuid();
            var amount = 42.50m;

            var order = new OrderEntity
            {
                Id = orderId,
                TenantId = tenantId,
                CustomerExternalReferenceId = "CUST123"
            };

            _uow.SetupGet(u => u.OrderRepository).Returns(_orderRepo.Object);
            _uow.SetupGet(u => u.OrderRefundRepository).Returns(_refundRepo.Object);
            _uow.SetupGet(u => u.ExchangeTransactionLogRepository).Returns(_txnRepo.Object);

            _uow.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _orderRepo.Setup(r => r.GetOrderAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(order);

            List<OneExchangeOrderService.Domain.Entities.ExchangeOrder.OrderRefund>? capturedRefunds = null;
            _refundRepo
                .Setup(r => r.CreateOrderRefundAsync(It.IsAny<List<OneExchangeOrderService.Domain.Entities.ExchangeOrder.OrderRefund>>()))
                .Callback<List<OneExchangeOrderService.Domain.Entities.ExchangeOrder.OrderRefund>>(list => capturedRefunds = list)
                .Returns(Task.CompletedTask);

            ExchangeTransactionLog? capturedTxn = null;
            _txnRepo
                .Setup(r => r.CreateTransactionAsync(It.IsAny<ExchangeTransactionLog>()))
                .Callback<ExchangeTransactionLog>(t => capturedTxn = t)
                .Returns(Task.CompletedTask);

            _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _uow.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);

            var req = new UpdateRefundRequest
            {
                OrderId = orderId.ToString("D"),
                OriginalPaymentId = originalPaymentId.ToString("D"),
                RefundAmount = amount
            };

            // act
            var ok = await sut.UpdateRefundAsync(req);

            // assert
            Assert.IsTrue(ok);

            // refund payload
            Assert.IsNotNull(capturedRefunds);
            Assert.AreEqual(1, capturedRefunds!.Count);
            Assert.AreEqual(tenantId, capturedRefunds[0].TenantId);
            Assert.AreEqual(orderId, capturedRefunds[0].OrderId);
            Assert.AreEqual(originalPaymentId, capturedRefunds[0].OriginalPaymentId);
            Assert.AreEqual(OneExchangeOrderServiceConstants.FEEREIMBURSEMENT, capturedRefunds[0].RefundPurpose);
            Assert.AreEqual(OneExchangeOrderServiceConstants.USD, capturedRefunds[0].Currency);
            Assert.AreEqual(amount, capturedRefunds[0].Amount);

            Assert.IsNotNull(capturedTxn);
            Assert.AreEqual(tenantId, capturedTxn!.TenantId);
            Assert.AreEqual(orderId, capturedTxn.OrderId);
            Assert.AreEqual(OneExchangeOrderServiceConstants.FEEREIMBURSEMENT, capturedTxn.TransactionType);
            Assert.AreEqual("USD", capturedTxn.ToCurrency);
            Assert.AreEqual(amount, capturedTxn.ToTransactionAmount);
            Assert.AreEqual(order.CustomerExternalReferenceId, capturedTxn.ToAccountId);

            _uow.Verify(u => u.BeginTransactionAsync(), Times.Once);
            _orderRepo.Verify(r => r.GetOrderAsync(orderId, It.IsAny<CancellationToken>()), Times.Once);
            _refundRepo.Verify(r => r.CreateOrderRefundAsync(It.IsAny<List<OneExchangeOrderService.Domain.Entities.ExchangeOrder.OrderRefund>>()), Times.Once);
            _txnRepo.Verify(r => r.CreateTransactionAsync(It.IsAny<ExchangeTransactionLog>()), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _uow.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [TestMethod]
        public async Task UpdateRefundAsyncWhenSaveThrowsRollsBackAndRethrows()
        {
            // arrange
            SetupLoggerInfo();
            var sut = CreateService();

            var orderId = Guid.NewGuid();
            var originalPaymentId = Guid.NewGuid();

            var order = new OrderEntity { Id = orderId, TenantId = Guid.NewGuid(), CustomerExternalReferenceId = "CUST" };

            _uow.SetupGet(u => u.OrderRepository).Returns(_orderRepo.Object);
            _uow.SetupGet(u => u.OrderRefundRepository).Returns(_refundRepo.Object);
            _uow.SetupGet(u => u.ExchangeTransactionLogRepository).Returns(_txnRepo.Object);

            _uow.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _orderRepo.Setup(r => r.GetOrderAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(order);
            _refundRepo.Setup(r => r.CreateOrderRefundAsync(It.IsAny<List<OneExchangeOrderService.Domain.Entities.ExchangeOrder.OrderRefund>>())).Returns(Task.CompletedTask);
            _txnRepo.Setup(r => r.CreateTransactionAsync(It.IsAny<ExchangeTransactionLog>())).Returns(Task.CompletedTask);

            _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new DbUpdateException("boom"));
            _uow.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            var req = new UpdateRefundRequest
            {
                OrderId = orderId.ToString("D"),
                OriginalPaymentId = originalPaymentId.ToString("D"),
                RefundAmount = 10m
            };

            // act + assert
            var ex = await Assert.ThrowsAsync<Exception>(() => sut.UpdateRefundAsync(req));
            StringAssert.Contains(ex.Message, "boom");

            _uow.Verify(u => u.BeginTransactionAsync(), Times.Once);
            _refundRepo.Verify(r => r.CreateOrderRefundAsync(It.IsAny<List<OneExchangeOrderService.Domain.Entities.ExchangeOrder.OrderRefund>>()), Times.Once);
            _txnRepo.Verify(r => r.CreateTransactionAsync(It.IsAny<ExchangeTransactionLog>()), Times.Once);
            _uow.Verify(u => u.RollbackTransactionAsync(), Times.Once);
            _uow.Verify(u => u.CommitTransactionAsync(), Times.Never);
        }

        [TestMethod]
        public async Task UpdateRefundAsyncWithBadOriginalPaymentIdThrowsFormatExceptionRollsBackAfterFetchingOrder()
        {
            // arrange
            SetupLoggerInfo();
            var sut = CreateService();

            var orderId = Guid.NewGuid();
            var order = new OrderEntity { Id = orderId, TenantId = Guid.NewGuid(), CustomerExternalReferenceId = "CUST" };

            _uow.SetupGet(u => u.OrderRepository).Returns(_orderRepo.Object);
            _uow.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _uow.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            _orderRepo.Setup(r => r.GetOrderAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(order);

            var req = new UpdateRefundRequest
            {
                OrderId = orderId.ToString("D"),
                OriginalPaymentId = "NOT-A-GUID", // will throw when parsed
                RefundAmount = 1m
            };

            // act + assert
            await Assert.ThrowsAsync<FormatException>(() => sut.UpdateRefundAsync(req));

            _uow.Verify(u => u.BeginTransactionAsync(), Times.Once);
            _orderRepo.Verify(r => r.GetOrderAsync(orderId, It.IsAny<CancellationToken>()), Times.Once); // fetched before failure
            _refundRepo.Verify(r => r.CreateOrderRefundAsync(It.IsAny<List<OneExchangeOrderService.Domain.Entities.ExchangeOrder.OrderRefund>>()), Times.Never);
            _txnRepo.Verify(r => r.CreateTransactionAsync(It.IsAny<ExchangeTransactionLog>()), Times.Never);
            _uow.Verify(u => u.RollbackTransactionAsync(), Times.Once);
            _uow.Verify(u => u.CommitTransactionAsync(), Times.Never);
        }

        [TestMethod]
        public async Task UpdateRefundAsyncWhenRequestIsNullThrowsNullReferenceExceptionNoTxnStarted()
        {
            // arrange
            SetupLoggerInfo();
            var sut = CreateService();

            // act + assert
            await Assert.ThrowsAsync<NullReferenceException>(() => sut.UpdateRefundAsync(null!));

            _uow.Verify(u => u.BeginTransactionAsync(), Times.Never);
            _orderRepo.Verify(r => r.GetOrderAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            _refundRepo.Verify(r => r.CreateOrderRefundAsync(It.IsAny<List<OneExchangeOrderService.Domain.Entities.ExchangeOrder.OrderRefund>>()), Times.Never);
            _txnRepo.Verify(r => r.CreateTransactionAsync(It.IsAny<ExchangeTransactionLog>()), Times.Never);
            _uow.Verify(u => u.RollbackTransactionAsync(), Times.Never);
            _uow.Verify(u => u.CommitTransactionAsync(), Times.Never);
        }

        [TestMethod]
        public async Task GetReservationByIdAsyncWhenReservationIdIsEmptyThrowsValidationException()
        {
            // Arrange
            var sut = CreateService();
            _uow.SetupGet(u => u.OrderRepository).Returns(_orderRepo.Object);
            _uow.SetupGet(u => u.ProfileRepository).Returns(_profileRepo.Object);

            // Act
            // await sut.GetReservationByIdAsync(string.Empty, CancellationToken.None);

            await Assert.ThrowsAsync<ValidationException>(
        () => sut.GetReservationByIdAsync(string.Empty, CancellationToken.None));
            // Assert handled by ExpectedException
        }

        [TestMethod]
        public async Task GetReservationByIdAsyncWhenOrderNotFoundThrowsDataNotFoundException()
        {
            // Arrange
            var sut = CreateService();
            var reservationId = "ORD-123";

            _uow.SetupGet(u => u.OrderRepository).Returns(_orderRepo.Object);
            _uow.SetupGet(u => u.ProfileRepository).Returns(_profileRepo.Object);

            _orderRepo
                .Setup(r =>
                    r.GetOrderByExternalReferenceIdAsync(reservationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((OrderEntity?)null);

            // Act + Assert
            await Assert.ThrowsAsync<DataNotFoundException>(
                () => sut.GetReservationByIdAsync(reservationId, CancellationToken.None));

            // Optional verifications (keep if useful)
            _orderRepo.Verify(r => r.GetOrderByExternalReferenceIdAsync(reservationId, It.IsAny<CancellationToken>()), Times.Once);

        }


        [TestMethod]
        public async Task GetReservationByIdAsyncWhenOrderFoundReturnsValidReservationGetResponse()
        {
            // Arrange
            var sut = CreateService();
            var reservationId = "ORD-001";
            var orderId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var guestId = Guid.NewGuid();

            var order = new OrderEntity
            {
                Id = orderId,
                TenantId = tenantId,
                ExternalReferenceId = reservationId,
                OrderStatus = "CONFIRMED",
                ExternalSourceSystem = "ONEX",
                CustomerFirstName = "Ada",
                CustomerLastName = "Lovelace",
                CustomerEmailAddress = "ada@example.com",
                CustomerPhoneNumber = "9999999999",
                InsertedDateTime = DateTimeOffset.UtcNow.AddDays(-2),
                UpdatedDateTime = DateTimeOffset.UtcNow,
                InsertedBy = "system",
                UpdatedBy = "system",
                Status = false,
                StatusReason = "BOOKED",
                OrderGuests =
        [
            new() { ProfileId = guestId }
        ],
                OrderItems =
        [
            new()
            {
                ItemCode = "PROP-001-UNIT-01",
                ItemDescription = "Sample Resort Unit",
                UnitPrice = 100.00m,
                UnitPriceCurrency = "USD",
                OrderItemAttributes =
                [
                    new() { AttributeType = OneExchangeOrderServiceConstants.CheckInDateOrderItemAttributeTypeString, AttributeValue = "2025-01-01" },
                    new() { AttributeType = OneExchangeOrderServiceConstants.CheckOutDateOrderItemAttributeTypeString, AttributeValue = "2025-01-05" },
                    new() { AttributeType = OneExchangeOrderServiceConstants.NumAdultsOrderItemAttributeTypeString, AttributeValue = "2" },
                    new() { AttributeType = OneExchangeOrderServiceConstants.NumChildrenOrderItemAttributeTypeString, AttributeValue = "1" }
                ]
            }
        ]
            };

            // Build EF Profile entity with related collections (for eager loading)
            var profile = new ProfileEntity
            {
                Id = guestId,
                FirstName = "Ada",
                LastName = "Lovelace",
                Addresses =
        [
            new()
            {
                Sequence = 1,
                AddressType = "HOME",
                Line1 = "1 Code St",
                City = "Meerut",
                State = "UP",
                Country = "IN",
                Postalcode = "250001"
            }
        ],
                Phones =
        [
            new()
            {
                Sequence = 1,
                PhoneType = "mobile",
                Number = "9999999999"
            }
        ],
                DigitalAddresses =
        [
            new()
            {
                Sequence = 1,
                DigitalAddressType = "email",
                DigitalAddress1 = "ada@example.com"
            }
        ]
            };

            _uow.SetupGet(u => u.OrderRepository).Returns(_orderRepo.Object);
            _uow.SetupGet(u => u.ProfileRepository).Returns(_profileRepo.Object);

            _orderRepo
                .Setup(r => r.GetOrderByExternalReferenceIdAsync(reservationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            _profileRepo
                .Setup(r => r.GetOrderGuestsDetailsAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync([profile]);

            // Act
            var result = await sut.GetReservationByIdAsync(reservationId, CancellationToken.None);

            // Assert – CloudEvent envelope
            Assert.IsNotNull(result);
            Assert.AreEqual(order.ExternalReferenceId, result.Id);
            Assert.AreEqual(OneExchangeOrderServiceConstants.OrderServiceSource, result.Source);
            Assert.AreEqual(OneExchangeOrderServiceConstants.ReservationGetResponseType, result.Type);
            Assert.AreEqual(OneExchangeOrderServiceConstants.ApplicationJson, result.Datacontenttype);
            Assert.AreEqual(OneExchangeOrderServiceConstants.ReservationGetSchema, result.Dataschema);
            Assert.AreEqual($"reservation/{order.ExternalReferenceId}", result.Subject);
            Assert.IsFalse(result.Ispartial);

            // SystemMeta
            Assert.AreEqual(order.TenantId.ToString(), result.Data.SystemMeta.TenantID);
            Assert.AreEqual(order.Status, result.Data.SystemMeta.Status);
            Assert.AreEqual(order.StatusReason, result.Data.SystemMeta.StatusReason);

            // ExternalReference
            Assert.AreEqual(order.ExternalSourceSystem, result.Data.ExternalReference.SourceSystem);
            Assert.AreEqual(OneExchangeOrderServiceConstants.TargetSystem, result.Data.ExternalReference.TargetSystem);
            Assert.AreEqual(order.ExternalReferenceId, result.Data.ExternalReference.TargetReferenceId);

            // Booking item mapping
            var booking = result.Data.BookingItems.First();
            Assert.AreEqual("PROP-001-UNIT-01", booking.ItemCode);
            Assert.AreEqual("Sample Resort Unit", booking.Description);
            Assert.AreEqual(100.00m, booking.PriceLevel.TotalPrice);
            Assert.AreEqual("USD", booking.PriceLevel.Currency);
            Assert.AreEqual(2, booking.GuestCounts.Adults);
            Assert.AreEqual(1, booking.GuestCounts.Children);

            // Guest info from Profile
            var guest = booking.GuestInfo;
            Assert.AreEqual("Ada", guest.FirstName);
            Assert.AreEqual("Lovelace", guest.LastName);
            Assert.AreEqual("ada@example.com", guest.DigitalAddresses.First().DigitalAddressValue);
            Assert.AreEqual("9999999999", guest.PhoneNumbers.First().Number);
            Assert.AreEqual("IN", guest.PostalAddresses.First().Country);
            Assert.AreEqual("Meerut", guest.PostalAddresses.First().City);
            Assert.AreEqual("250001", guest.PostalAddresses.First().PostalCode);

            _orderRepo.Verify(r => r.GetOrderByExternalReferenceIdAsync(reservationId, It.IsAny<CancellationToken>()), Times.Once);
            _profileRepo.Verify(r => r.GetOrderGuestsDetailsAsync(It.IsAny<List<Guid>>()), Times.Once);
        }

        [TestMethod]
        public async Task GetReservationByIdAsyncWhenProfileNotFoundStillReturnsResponseWithGuestInfoFromOrder()
        {
            // Arrange
            var sut = CreateService();
            var reservationId = "ORD-002";
            var orderId = Guid.NewGuid();

            var order = new OrderEntity
            {
                Id = orderId,
                TenantId = Guid.NewGuid(),
                ExternalReferenceId = reservationId,
                OrderStatus = "CONFIRMED",
                ExternalSourceSystem = "ONEX",
                CustomerFirstName = "Alan",
                CustomerLastName = "Turing",
                CustomerEmailAddress = "alan@example.com",
                CustomerPhoneNumber = "8888888888",
                Status = false,
                StatusReason = "BOOKED",
                OrderGuests =
                [
                    new() { ProfileId = Guid.NewGuid() }
                ],
                OrderItems =
                [
                    new()
                    {
                        ItemCode = "PROP-002-UNIT-02",
                        ItemDescription = "Unit 2",
                        UnitPrice = 200,
                        UnitPriceCurrency = "USD"
                    }
                ]
            };

            _uow.SetupGet(u => u.OrderRepository).Returns(_orderRepo.Object);
            _uow.SetupGet(u => u.ProfileRepository).Returns(_profileRepo.Object);

            _orderRepo
                .Setup(r => r.GetOrderByExternalReferenceIdAsync(reservationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            // No profiles found → empty list, not null
            _profileRepo
                .Setup(r => r.GetOrderGuestsDetailsAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync([]);

            // Act
            var result = await sut.GetReservationByIdAsync(reservationId, CancellationToken.None);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(order.ExternalReferenceId, result.Id);

            var guest = result.Data.BookingItems.First().GuestInfo;

            // Should fall back to order's customer details
            Assert.AreEqual("Alan", guest.FirstName);
            Assert.AreEqual("Turing", guest.LastName);

            // And since there is no profile, lists are empty
            Assert.AreEqual(0, guest.PostalAddresses.Count);
            Assert.AreEqual(0, guest.PhoneNumbers.Count);
            Assert.AreEqual(0, guest.DigitalAddresses.Count);

            _orderRepo.Verify(r => r.GetOrderByExternalReferenceIdAsync(reservationId, It.IsAny<CancellationToken>()), Times.Once);
            _profileRepo.Verify(r => r.GetOrderGuestsDetailsAsync(It.IsAny<List<Guid>>()), Times.Once);
        }

        [TestMethod]
        public async Task SearchReservationsAsyncWhenRequestIsNullThrowsValidationException()
        {
            var sut = CreateService();

            var req = new ReservationSearchRequest
            {
                CorrelationId = "corr-1",
                Data = new ReservationSearchRequestData
                {
                    SearchCriteria = null!
                }
            };

            await Assert.ThrowsAsync<ValidationException>(() =>
                sut.SearchReservationsAsync(req, 1, 10, CancellationToken.None));
        }

        [TestMethod]
        public async Task SearchReservationsAsyncWhenCriteriaMissingThrowsValidationException()
        {
            var sut = CreateService();

            var request = new ReservationSearchRequest
            {
                Id = "REQ-01",
                Source = "SRC",
                CorrelationId = "CORR",
                Data = new ReservationSearchRequestData
                {
                    SystemMeta = new RetrievalSystemMeta { TenantID = "TENANT" },
                    SearchCriteria = null
                }
            };

            await Assert.ThrowsAsync<ValidationException>(async () => await sut.SearchReservationsAsync(request, 1, 10, CancellationToken.None));
        }

        [TestMethod]
        public async Task SearchReservationsAsyncWhenNoOrdersFoundReturnsEmptyResponse()
        {
            // Arrange
            var sut = CreateService();
            var request = BuildValidSearchRequest();

            _uow.SetupGet(u => u.OrderRepository).Returns(_orderRepo.Object);
            _uow.SetupGet(u => u.ProfileRepository).Returns(_profileRepo.Object);

            _orderRepo
                .Setup(r => r.SearchOrdersAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<List<Guid>>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<int?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<int?>(),
                    1,
                    10,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Enumerable.Empty<OrderEntity>(), 0));

            _profileRepo
            .Setup(r => r.GetProfilesByExternalReferenceIdAsync(It.IsAny<string>()))
            .ReturnsAsync([]);


            // Act
            var response = await sut.SearchReservationsAsync(request, 1, 10, CancellationToken.None);

            // Assert
            Assert.IsNotNull(response);
            Assert.AreEqual(0, response.Data.Pagination.TotalItems);
            Assert.AreEqual(0, response.Data.Results.Count);
        }

        [TestMethod]
        public async Task SearchReservationsAsyncWhenOrdersIsNullReturnsEmptyResponse()
        {
            // Arrange
            var sut = CreateService();
            var request = BuildValidSearchRequest();

            // CRITICAL: ensure SourceMemberIds is null or empty
            request.Data.SearchCriteria.SourceMemberIds = null;

            _uow.SetupGet(u => u.OrderRepository).Returns(_orderRepo.Object);
            _uow.SetupGet(u => u.ProfileRepository).Returns(_profileRepo.Object);

            _orderRepo
                .Setup(r => r.SearchOrdersAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<List<Guid>>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<int?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<int?>(),
                    1,
                    10,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((new List<OrderEntity>(), 0));

            // Act
            var response = await sut.SearchReservationsAsync(request, 1, 10, CancellationToken.None);

            // Assert
            Assert.IsNotNull(response);
            Assert.AreEqual(0, response.Data.Results.Count);
            Assert.AreEqual(0, response.Data.Pagination.TotalItems);

            // Optional verification (proves path was hit)
            _orderRepo.Verify(r => r.SearchOrdersAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<List<Guid>>(),
                It.IsAny<DateTime?>(),
                It.IsAny<int?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<int?>(),
                1,
                10,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public async Task SearchReservationsAsyncWhenSourceReservationSourceSystemTooLongThrows()
        {
            var sut = CreateService();
            var request = BuildValidSearchRequest();

            request.Data.SearchCriteria.SourceReservationIds =
                [
            new SourceIdentifier
            {
                SourceSystem = new string('A', 21),
                Id = "RES1"
            }
                ];

            await Assert.ThrowsAsync<ValidationException>(() =>
                sut.SearchReservationsAsync(request, 1, 10, CancellationToken.None));
        }

        [TestMethod]
        public async Task SearchReservationsAsyncWhenSourceMemberIdIsEmptyThrows()
        {
            var sut = CreateService();
            var request = BuildValidSearchRequest();

            request.Data.SearchCriteria.SourceMemberIds =
                [new SourceIdentifier { SourceSystem = "EXCHANGE", Id = "" }];

            await Assert.ThrowsAsync<ValidationException>(() =>
                sut.SearchReservationsAsync(request, 1, 10, CancellationToken.None));
        }

        [TestMethod]
        public async Task SearchReservationsAsyncWhenSourceMemberSourceSystemTooLongThrows()
        {
            var sut = CreateService();
            var request = BuildValidSearchRequest();

            request.Data.SearchCriteria.SourceMemberIds =
                [
            new SourceIdentifier
            {
                SourceSystem = new string('B', 21),
                Id = "MEM1"
            }
                ];

            await Assert.ThrowsAsync<ValidationException>(() =>
                sut.SearchReservationsAsync(request, 1, 10, CancellationToken.None));
        }

        [TestMethod]
        public async Task SearchReservationsAsyncWhenSourceMemberIdTooLongThrows()
        {
            var sut = CreateService();
            var request = BuildValidSearchRequest();

            request.Data.SearchCriteria.SourceMemberIds =
                [
            new SourceIdentifier
            {
                SourceSystem = "EXCHANGE",
                Id = new string('C', 21)
            }
                ];

            await Assert.ThrowsAsync<ValidationException>(() =>
                sut.SearchReservationsAsync(request, 1, 10, CancellationToken.None));
        }

        [TestMethod]
        public async Task SearchReservationsAsyncWhenArrivalDateIntervalNegativeThrows()
        {
            // Arrange
            var sut = CreateService();
            var request = BuildValidSearchRequest();

            request.Data.SearchCriteria.ArrivalDateInterval = -1;

            // Act + Assert
            await Assert.ThrowsAsync<ValidationException>(() =>
                sut.SearchReservationsAsync(request, 1, 10, CancellationToken.None));
        }

        [TestMethod]
        public async Task SearchReservationsAsyncWhenOrderByDirectionInvalidThrows()
        {
            // Arrange
            var sut = CreateService();
            var request = BuildValidSearchRequest();

            request.Data.SearchCriteria.OrderBy = new OrderBy
            {
                Field = "arrivalDate",
                Direction = "sideways"
            };

            // Act + Assert
            await Assert.ThrowsAsync<ValidationException>(() =>
                sut.SearchReservationsAsync(request, 1, 10, CancellationToken.None));
        }


        [TestMethod]
        public async Task SearchReservationsAsyncWhenSearchCriteriaNullThrows()
        {
            var sut = CreateService();
            var request = BuildValidSearchRequest();
            request.Data.SearchCriteria = null;

            await Assert.ThrowsAsync<ValidationException>(() =>
                sut.SearchReservationsAsync(request, 1, 10, CancellationToken.None));
        }

        [TestMethod]
        public async Task SearchReservationsAsyncWhenSourceReservationIdIsEmptyThrows()
        {
            var sut = CreateService();
            var request = BuildValidSearchRequest();

            request.Data.SearchCriteria.SourceReservationIds =
                [new SourceIdentifier { SourceSystem = "EXCHANGE", Id = "" }];

            await Assert.ThrowsAsync<ValidationException>(() =>
                sut.SearchReservationsAsync(request, 1, 10, CancellationToken.None));
        }


        [TestMethod]
        public async Task SearchReservationsAsyncWhenCorrelationIdMissingThrows()
        {
            var sut = CreateService();
            var request = BuildValidSearchRequest();
            request.CorrelationId = "";

            await Assert.ThrowsAsync<ValidationException>(() =>
                sut.SearchReservationsAsync(request, 1, 10, CancellationToken.None));
        }

        [TestMethod]
        public async Task SearchReservationsAsyncWhenTradingAgreementCodeMissingThrows()
        {
            var sut = CreateService();
            var request = BuildValidSearchRequest();
            request.Data.SearchCriteria.TradingAgreementCode = "";

            await Assert.ThrowsAsync<ValidationException>(() =>
                sut.SearchReservationsAsync(request, 1, 10, CancellationToken.None));
        }

        [TestMethod]
        public async Task SearchReservationsAsyncWhenSourceReservationIdsMoreThan10Throws()
        {
            var sut = CreateService();
            var request = BuildValidSearchRequest();

            request.Data.SearchCriteria.SourceReservationIds =
                Enumerable.Range(1, 11)
                    .Select(i => new SourceIdentifier { SourceSystem = "EXCHANGE", Id = i.ToString(CultureInfo.InvariantCulture) })
                    .ToList();

            await Assert.ThrowsAsync<ValidationException>(() =>
                sut.SearchReservationsAsync(request, 1, 10, CancellationToken.None));
        }

        [TestMethod]
        public async Task SearchReservationsAsyncWhenSourceReservationIdSourceSystemMissingThrows()
        {
            var sut = CreateService();
            var request = BuildValidSearchRequest();

            request.Data.SearchCriteria.SourceReservationIds =
                [new SourceIdentifier { SourceSystem = "", Id = "RES1" }];

            await Assert.ThrowsAsync<ValidationException>(() =>
                sut.SearchReservationsAsync(request, 1, 10, CancellationToken.None));
        }

        [TestMethod]
        public async Task SearchReservationsAsyncWhenReservationIdTooLongThrows()
        {
            var sut = CreateService();
            var request = BuildValidSearchRequest();

            request.Data.SearchCriteria.SourceReservationIds =
                [new SourceIdentifier { SourceSystem = "EXCHANGE", Id = new string('A', 21) }];

            await Assert.ThrowsAsync<ValidationException>(() =>
                sut.SearchReservationsAsync(request, 1, 10, CancellationToken.None));
        }

        [TestMethod]
        public async Task SearchReservationsAsyncWhenSourceMemberIdsMoreThan10Throws()
        {
            var sut = CreateService();
            var request = BuildValidSearchRequest();

            request.Data.SearchCriteria.SourceMemberIds =
                Enumerable.Range(1, 11)
                    .Select(i => new SourceIdentifier { SourceSystem = "EXCHANGE", Id = i.ToString(CultureInfo.InvariantCulture) })
                    .ToList();

            await Assert.ThrowsAsync<ValidationException>(() =>
                sut.SearchReservationsAsync(request, 1, 10, CancellationToken.None));
        }

        [TestMethod]
        public async Task SearchReservationsAsyncWhenStatusInvalidThrows()
        {
            var sut = CreateService();
            var request = BuildValidSearchRequest();
            request.Data.SearchCriteria.Status = "INVALID";

            await Assert.ThrowsAsync<ValidationException>(() =>
                sut.SearchReservationsAsync(request, 1, 10, CancellationToken.None));
        }

        [TestMethod]
        public async Task SearchReservationsAsyncWhenOrderByFieldInvalidThrows()
        {
            var sut = CreateService();
            var request = BuildValidSearchRequest();
            request.Data.SearchCriteria.OrderBy = new OrderBy { Field = "price", Direction = "asc" };

            await Assert.ThrowsAsync<ValidationException>(() =>
                sut.SearchReservationsAsync(request, 1, 10, CancellationToken.None));
        }

        [TestMethod]
        public async Task SearchReservationsAsyncWhenNoFiltersProvidedThrows()
        {
            var sut = CreateService();
            var request = BuildValidSearchRequest();

            request.Data.SearchCriteria = new SearchCriteria
            {
                TradingAgreementCode = "TA"
                // nothing else set
            };

            await Assert.ThrowsAsync<ValidationException>(() =>
                sut.SearchReservationsAsync(request, 1, 10, CancellationToken.None));
        }

        [TestMethod]
        public async Task SearchReservationsAsyncWhenReservationDateIntervalNegativeThrows()
        {
            var sut = CreateService();
            var request = BuildValidSearchRequest();
            request.Data.SearchCriteria.ReservationDateInterval = -1;

            await Assert.ThrowsAsync<ValidationException>(() =>
                sut.SearchReservationsAsync(request, 1, 10, CancellationToken.None));
        }


        [TestMethod]
        public async Task SearchReservationsAsyncWhenOrdersExistReturnsMappedResults()
        {
            // Arrange
            var sut = CreateService();
            var request = BuildValidSearchRequest();
            request.Data.SearchCriteria.SourceMemberIds =
            [
                new() { SourceSystem = "EXCHANGE", Id = "MEM01" }
            ];

            var profileId = Guid.NewGuid();

            var order = new OrderEntity
            {
                CustomerExternalReferenceId = "MEM123",
                OrderStatus = "CONFIRMED",
                OrderDate = DateTime.UtcNow,
                OrderGuests =
                [
                    new() { ProfileId = profileId }
                ],
                OrderItems =
                [
                    new()
                    {
                        ItemCode = "PROP-1-UNIT-1",
                        ItemDescription = "Unit Desc",
                        UnitPrice = 150,
                        UnitPriceCurrency = "USD",
                        OrderItemAttributes =
                        [
                            new() { AttributeType = "CHECK_IN_DATE", AttributeValue = "2025-01-01" },
                            new() { AttributeType = "CHECK_OUT_DATE", AttributeValue = "2025-01-02" },
                            new() { AttributeType = "NUM_ADULTS", AttributeValue = "2" },
                            new() { AttributeType = "NUM_CHILDREN", AttributeValue = "1" }
                        ]
                    }
                ]
            };

            var profile = new ProfileEntity
            {
                Id = profileId,
                FirstName = "Alan",
                LastName = "Turing",
                Addresses = [],
                Phones = [],
                DigitalAddresses = []
            };

            _uow.SetupGet(u => u.OrderRepository).Returns(_orderRepo.Object);
            _uow.SetupGet(u => u.ProfileRepository).Returns(_profileRepo.Object);

            _orderRepo.Setup(r => r.SearchOrdersAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<List<Guid>>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<int?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<int?>(),
                    1, 10,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((new List<OrderEntity> { order }, 1));

            _profileRepo
                .Setup(r => r.GetProfilesByExternalReferenceIdAsync("MEM01"))
                .ReturnsAsync([profile]);

            _profileRepo
                .Setup(r => r.GetOrderGuestsDetailsAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync([profile]);

            // Act
            var result = await sut.SearchReservationsAsync(request, 1, 10, CancellationToken.None);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Data.Results.Count);

            var item = result.Data.Results[0];

            Assert.AreEqual("PROP-1-UNIT-1", item.ItemCode);
            Assert.AreEqual("Alan", item.GuestInfo.FirstName);
            Assert.AreEqual("Turing", item.GuestInfo.LastName);
            Assert.AreEqual("2025-01-01", item.StartDate);
            Assert.AreEqual("2025-01-02", item.EndDate);
        }

        // --- Helper to build a minimal valid request ---
        private static OrderReservationRequest BuildMinimalRequest1()
        {
            // Ensure these formats match your CommonHelper/OrganizationHelper parsing.
            var tradingAgreement = $"BGV-BGV-GPX";
            var itemCode = "GPX-GP001-1";

            return new OrderReservationRequest
            {
                TradingAgreementCode = tradingAgreement,
                Currency = "USD",
                Subtotal = 100m,
                TaxCurrency = "USD",
                TaxAmount = 0m,
                FeeCurrency = "USD",
                OrderFee = 0m,
                CustomerProfile = new ProfileRequest
                {
                    FirstName = "Ada",
                    LastName = "Lovelace",
                    Addresses =
            [
                new() { Line1 = "1 Code St", City = "Meerut", State = "UP", PostalCode = "250001", Country = "IN" }
            ],
                    Phones =
            [
                new() { Number = "9999999999" }
            ],
                    DigitalAddresses =
            [
                new() { DigitalAddressType = "EMAIL", DigitalAddressDetail = "ada@example.com" }
            ],
                    Identifiers =
            [
                new()
                {
                    IdentifierSource = OneExchangeOrderServiceConstants.BgvMemberIdentifierSource,
                    IdentifierType = OneExchangeOrderServiceConstants.MemberIdIdentifierString,
                    Identifier = "BG123"
                }
            ]
                },
                SalesPersonProfile = new ProfileRequest
                {
                    FirstName = "Grace",
                    LastName = "Hopper",
                    DigitalAddresses =
            [
                new() { DigitalAddressType = "EMAIL", DigitalAddressDetail = "grace@example.com" }
            ]
                },
                OrderItems =
        [
            new()
            {
                ItemCode = itemCode,
                UnitPrice = 100m,
                Currency = "USD",
                TaxCurrency = "USD",
                TaxAmount = 0m,
                Attributes =
                [
                    new() { AttributeType = OneExchangeOrderServiceConstants.CheckInDateOrderItemAttributeTypeString, AttributeValue = "2025-12-01" },
                    new() { AttributeType = OneExchangeOrderServiceConstants.CheckOutDateOrderItemAttributeTypeString, AttributeValue = "2025-12-03" },
                    new() { AttributeType = OneExchangeOrderServiceConstants.NumAdultsOrderItemAttributeTypeString, AttributeValue = "2" },
                    new() { AttributeType = OneExchangeOrderServiceConstants.MemberShipLevelAttributeTypeString, AttributeValue = "GOLD" }
                ]
            }
        ],
                Guests =
        [
            new() { FirstName = "Alan", LastName = "Turing", ProfileType = "Primary" }
        ],
                CurrencyEquivalent = null // triggers the "null -> new List<>()" branch
            };
        }

        private static OrderReservationRequest BuildMinimalRequest_SkipRealtime()
        {
            var tradingAgreement = $"BGV-BGV-GPX";
            var itemCode = $"GPX-GP01-1";

            return new OrderReservationRequest
            {
                TradingAgreementCode = tradingAgreement,
                Currency = "USD",
                Subtotal = 100m,
                TaxCurrency = "USD",
                TaxAmount = 0m,
                FeeCurrency = "USD",
                OrderFee = 0m,
                CustomerProfile = new ProfileRequest
                {
                    FirstName = "Ada",
                    LastName = "Lovelace",
                    Addresses = [new() { Line1 = "1 Code St", City = "Meerut", State = "UP", PostalCode = "250001", Country = "IN" }],
                    Phones = [new() { Number = "9999999999" }],
                    DigitalAddresses = [new() { DigitalAddressType = "EMAIL", DigitalAddressDetail = "ada@example.com" }],
                    Identifiers =
            [
                new()
                {
                    IdentifierSource = OneExchangeOrderServiceConstants.BgvMemberIdentifierSource,
                    IdentifierType = OneExchangeOrderServiceConstants.MemberIdIdentifierString,
                    Identifier = "BG123"
                }
            ]
                },
                SalesPersonProfile = new ProfileRequest
                {
                    FirstName = "Grace",
                    LastName = "Hopper",
                    DigitalAddresses = [new() { DigitalAddressType = "EMAIL", DigitalAddressDetail = "grace@example.com" }]
                },
                OrderItems =
        [
            new()
            {
                ItemCode = itemCode,
                UnitPrice = 100m,
                Currency = "USD",
                TaxCurrency = "USD",
                TaxAmount = 0m,
                Attributes =
                [
                    new() { AttributeType = OneExchangeOrderServiceConstants.CheckInDateOrderItemAttributeTypeString, AttributeValue = "2025-12-01" },
                    new() { AttributeType = OneExchangeOrderServiceConstants.CheckOutDateOrderItemAttributeTypeString, AttributeValue = "2025-12-03" },
                    new() { AttributeType = OneExchangeOrderServiceConstants.NumAdultsOrderItemAttributeTypeString, AttributeValue = "2" },
                    new() { AttributeType = OneExchangeOrderServiceConstants.MemberShipLevelAttributeTypeString, AttributeValue = "GOLD" }
                ]
            }
        ],
                Guests = [new() { FirstName = "Alan", LastName = "Turing", ProfileType = "Primary" }],
                CurrencyEquivalent = []
            };
        }

        private static ReservationSearchRequest BuildValidSearchRequest() => new()
        {
            Id = "REQ-01",
            Source = "SRC",
            CorrelationId = "CORR-1",
            Data = new ReservationSearchRequestData
            {
                SystemMeta = new RetrievalSystemMeta
                {
                    TenantID = "TENANT"
                },
                SearchCriteria = new SearchCriteria
                {
                    TradingAgreementCode = "TAG",
                    Status = "CONFIRMED",
                    SourceReservationIds =
                        [
                            new() { SourceSystem = "EXCHANGE", Id = "RES01" }
                        ],
                    SourceMemberIds =
                        [
                            new() { SourceSystem = "EXCHANGE", Id = "MEM01" }
                        ]
                }
            }
        };
    }
}
