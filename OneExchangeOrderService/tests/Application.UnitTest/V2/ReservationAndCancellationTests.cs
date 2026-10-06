using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using OneExchangeOrderService.Application.Eventhub;
using OneExchangeOrderService.Application.Interfaces;
using OneExchangeOrderService.Application.Interfaces.Logic;
using OneExchangeOrderService.Domain.Entities.ExchangeOrder;
using OneExchangeOrderService.Domain.Exceptions;
using OneExchangeOrderService.Domain.Models.V2.Cancellation.Request;
using OneExchangeOrderService.Domain.Models.V2.Cancellation.Response;
using OneExchangeOrderService.Domain.Models.V2.Reservation.Request;
using OneExchangeOrderService.Domain.Models.V2.Reservation.ReservationConnector.Request;
using OneExchangeOrderService.Domain.Models.V2.Reservation.ReservationConnector.Response;
using OneExchangeOrderService.Domain.Persistence.Repositories.ExchangeOrder;
using OneExchangeOrderService.Domain.Persistence.Repositories.ExchangeProfile;
using OneExchangeOrderService.Domain.Persistence.Repositories.ExchangeReservation;
using OneExchangeOrderService.DTO.Models.InventoryUpdate.Request;
using OneExchangeOrderService.DTO.Models.InventoryUpdate.Response;
using OneExchangeOrderService.Infrastructure.Persistance.Repositories.ExchangeOrder;
using System.Text.Json;
using Profile = OneExchangeOrderService.Domain.Entities.ExchangeProfile.Profile;

namespace Application.UnitTests.Interfaces.Logic
{
    [TestClass]
    public class ReservationAndCancellation
    {
        private Mock<IOrderRepository> _orderRepository;
        private Mock<IOrderReturnRepository> _orderReturnRepository;
        private Mock<IExchangeTransactionLogRepository> _transactionRepository;
        private Mock<IProfileRepository> _profileRepository;
        private Mock<ILogger<OrderService>> _logger;
        private Mock<IOrderUnitOfWork> _uow;
        private Mock<IOneExchangeUnitOfWork> _reservationUow = default!;
        private Mock<IOneExchangeOrderRepository> _reservationOrderRepo = default!;
        private Mock<IOneExchangeProfileRepository> _reservationProfileRepo = default!;
        private Mock<IOrderReservationEventPublisher> _eventPublisher;
        private Mock<IOrderPaymentRepository> _orderPaymentRepository;
        private OrderCancelEventPublisher _eventCancelOrderPublisher;
        private Mock<IContentfulService> _contentfulService;
        private Mock<IMapper> _mapper;
        private Mock<IInRuleService> _inRuleService;
        private Mock<IInventoryServiceClient> _inventoryService;
        private Mock<IBoomiConnector> _boomiConnector;
        private OrderService _sut;

        [TestInitialize]
        public void Init()
        {
            _orderRepository = new Mock<IOrderRepository>(MockBehavior.Loose);
            _orderReturnRepository = new Mock<IOrderReturnRepository>(MockBehavior.Loose);
            _transactionRepository = new Mock<IExchangeTransactionLogRepository>(MockBehavior.Loose);
            _profileRepository = new Mock<IProfileRepository>(MockBehavior.Loose);
            _logger = new Mock<ILogger<OrderService>>(MockBehavior.Loose);
            _uow = new Mock<IOrderUnitOfWork>(MockBehavior.Loose);

            // ⭐ Setup Reservation UoW
            _reservationOrderRepo = new Mock<IOneExchangeOrderRepository>(MockBehavior.Loose);
            _reservationProfileRepo = new Mock<IOneExchangeProfileRepository>(MockBehavior.Loose);
            _reservationUow = new Mock<IOneExchangeUnitOfWork>(MockBehavior.Loose);
            _reservationUow.SetupGet(x => x.Orders).Returns(_reservationOrderRepo.Object);
            _reservationUow.SetupGet(x => x.Profiles).Returns(_reservationProfileRepo.Object);
            _reservationUow.Setup(x => x.SaveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _eventPublisher = new Mock<IOrderReservationEventPublisher>(MockBehavior.Loose);
            _orderPaymentRepository = new Mock<IOrderPaymentRepository>(MockBehavior.Loose);
            _eventCancelOrderPublisher = null;
            _contentfulService = new Mock<IContentfulService>(MockBehavior.Loose);
            _mapper = new Mock<IMapper>(MockBehavior.Loose);
            _inRuleService = new Mock<IInRuleService>(MockBehavior.Loose);
            _inventoryService = new Mock<IInventoryServiceClient>(MockBehavior.Loose);
            _boomiConnector = new Mock<IBoomiConnector>(MockBehavior.Loose);

            _sut = new OrderService(
                _orderRepository.Object,
                _orderReturnRepository.Object,
                _transactionRepository.Object,
                _profileRepository.Object,
                _logger.Object,
                _uow.Object,
                _eventPublisher.Object,
                _orderPaymentRepository.Object,
                _eventCancelOrderPublisher,
                _contentfulService.Object,
                _mapper.Object,
                _inRuleService.Object,
                _inventoryService.Object,
                _boomiConnector.Object,
                _reservationUow.Object
            );
        }

        // ------------------- RESERVATION TESTS  -------------------

        [TestMethod]
        public async Task CreateReservationAsyncHappyPathReturnsConfirmed()
        {
            var request = JsonSerializer.Deserialize<ReservationRequest>(SAMPLE_RESERVATION_REQUEST_JSON);

            _reservationProfileRepo.Setup(r => r.CreateProfileAsync(It.IsAny<Profile>()));
            _reservationOrderRepo.Setup(r => r.CreateOrderAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);

            var boomiResp = new ReservationConnectorResponse
            {
                Data = new ReservationConnectorResponseData
                {
                    BookingId = "BOOK-123",
                    ExternalReference = new OneExchangeOrderService.Domain.Models.V2.Reservation.ReservationConnector.Request.ExternalReference
                    {
                        TargetReferenceId = "PMS123"
                    }
                }
            };
            _boomiConnector.Setup(x => x.ReservationConnectorCreateAsync(It.IsAny<ReservationConnectorRequest>()))
                .ReturnsAsync(boomiResp);

            _inventoryService.Setup(x => x.UpdateInventoryAsync(It.IsAny<InventoryRequest>()))
                .ReturnsAsync(new InventoryResponse { Success = true });

            var result = await _sut.CreateReservationAsync(request, CancellationToken.None);

            Assert.IsNotNull(result);
            Assert.AreEqual("CONFIRMED", result.Data.BookingStatus);
            _reservationUow.Verify(x => x.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task CreateReservationAsyncWhenBookingItemsNullThrowsArgumentException()
        {
            var request = JsonSerializer.Deserialize<ReservationRequest>(SAMPLE_RESERVATION_REQUEST_JSON);
            request.Data.BookingItems = null;

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _sut.CreateReservationAsync(request, CancellationToken.None));
        }

        [TestMethod]
        public async Task CreateReservationAsyncWhenDataIsNullThrowsArgumentException()
        {
            var request = JsonSerializer.Deserialize<ReservationRequest>(SAMPLE_RESERVATION_REQUEST_JSON);
            Assert.IsNotNull(request);
            request.Data = null!;

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _sut.CreateReservationAsync(request, CancellationToken.None));
        }

        [TestMethod]
        public async Task CreateReservationAsyncWhenBookingItemsEmptyThrowsArgumentException()
        {
            var request = JsonSerializer.Deserialize<ReservationRequest>(SAMPLE_RESERVATION_REQUEST_JSON);
            Assert.IsNotNull(request);
            Assert.IsNotNull(request.Data);
            request.Data.BookingItems = [];

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _sut.CreateReservationAsync(request, CancellationToken.None));
        }

        [TestMethod]
        public async Task CreateReservationAsyncWhenBoomiThrowsReturnsFailure()
        {
            var request = JsonSerializer.Deserialize<ReservationRequest>(SAMPLE_RESERVATION_REQUEST_JSON);

            _boomiConnector
                .Setup(x => x.ReservationConnectorCreateAsync(It.IsAny<ReservationConnectorRequest>()))
                .ThrowsAsync(new HttpRequestException("Boomi error"));

            await Assert.ThrowsAsync<BoomiTimeoutException>(() =>
                _sut.CreateReservationAsync(request!, CancellationToken.None));
        }

        [TestMethod]
        public async Task CreateReservationAsyncWhenInventoryUpdateFailsReturnsFailure()
        {
            var request = JsonSerializer.Deserialize<ReservationRequest>(SAMPLE_RESERVATION_REQUEST_JSON);

            _reservationProfileRepo.Setup(r => r.CreateProfileAsync(It.IsAny<Profile>()));
            _reservationOrderRepo.Setup(r => r.CreateOrderAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);

            var boomiResp = new ReservationConnectorResponse
            {
                Data = new ReservationConnectorResponseData
                {
                    BookingId = "BOOK-123",
                    ExternalReference = new OneExchangeOrderService.Domain.Models.V2.Reservation.ReservationConnector.Request.ExternalReference
                    {
                        TargetReferenceId = "PMS123"
                    }
                }
            };
            _boomiConnector.Setup(x => x.ReservationConnectorCreateAsync(It.IsAny<ReservationConnectorRequest>()))
                .ReturnsAsync(boomiResp);

            _inventoryService.Setup(x => x.UpdateInventoryAsync(It.IsAny<InventoryRequest>()))
                .ReturnsAsync(new InventoryResponse { Success = false, Message = "Inventory error" });

            var result = await _sut.CreateReservationAsync(request, CancellationToken.None);

            Assert.AreEqual("FAILED", result.Data.BookingStatus);
            Assert.AreEqual("INVENTORY_FAILED", result.Data.Error.ErrorCode);
        }

        [TestMethod]
        public async Task CreateReservationAsyncWhenBoomiReturnsNoBookingIdReturnsFailure()
        {
            var request = JsonSerializer.Deserialize<ReservationRequest>(SAMPLE_RESERVATION_REQUEST_JSON);

            var boomiResp = new ReservationConnectorResponse
            {
                Data = new ReservationConnectorResponseData
                {
                    BookingId = null,
                    Error = new OneExchangeOrderService.Domain.Models.V2.Reservation.Response.ErrorInfo
                    {
                        ErrorCode = "BOOMI_ERR",
                        ErrorMessage = "Boomi error"
                    }
                }
            };
            _boomiConnector.Setup(x => x.ReservationConnectorCreateAsync(It.IsAny<ReservationConnectorRequest>()))
                .ReturnsAsync(boomiResp);

            var result = await _sut.CreateReservationAsync(request, CancellationToken.None);

            Assert.AreEqual("FAILED", result.Data.BookingStatus);
            Assert.AreEqual("BOOMI_ERR", result.Data.Error.ErrorCode);
        }

        [TestMethod]
        public async Task CreateReservationAsyncWhenInventoryUpdateReturnsNullReturnsFailure()
        {
            var request = JsonSerializer.Deserialize<ReservationRequest>(SAMPLE_RESERVATION_REQUEST_JSON);

            _reservationProfileRepo.Setup(r => r.CreateProfileAsync(It.IsAny<Profile>()));
            _reservationOrderRepo.Setup(r => r.CreateOrderAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);

            var boomiResp = new ReservationConnectorResponse
            {
                Data = new ReservationConnectorResponseData
                {
                    BookingId = "BOOK-123",
                    ExternalReference = new OneExchangeOrderService.Domain.Models.V2.Reservation.ReservationConnector.Request.ExternalReference
                    {
                        TargetReferenceId = "PMS123"
                    }
                }
            };
            _boomiConnector.Setup(x => x.ReservationConnectorCreateAsync(It.IsAny<ReservationConnectorRequest>()))
                .ReturnsAsync(boomiResp);

            _inventoryService.Setup(x => x.UpdateInventoryAsync(It.IsAny<InventoryRequest>()))
                .ReturnsAsync(new InventoryResponse { Success = false, Message = string.Empty });

            var result = await _sut.CreateReservationAsync(request, CancellationToken.None);

            Assert.AreEqual("FAILED", result.Data.BookingStatus);
            Assert.AreEqual("INVENTORY_FAILED", result.Data.Error.ErrorCode);
        }

        [TestMethod]
        public async Task CreateReservationAsyncWhenSaveChangesThrowsThrows()
        {
            var request = JsonSerializer.Deserialize<ReservationRequest>(SAMPLE_RESERVATION_REQUEST_JSON);
            Assert.IsNotNull(request); // fixes nullable warning

            _reservationProfileRepo.Setup(r => r.CreateProfileAsync(It.IsAny<Profile>()));
            _reservationOrderRepo.Setup(r => r.CreateOrderAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
            _reservationUow.Setup(x => x.SaveAsync(It.IsAny<CancellationToken>()))
                           .ThrowsAsync(new DbUpdateException("DB error"));

            var boomiResp = new ReservationConnectorResponse
            {
                Data = new ReservationConnectorResponseData
                {
                    BookingId = "BOOK-123",
                    ExternalReference = new OneExchangeOrderService.Domain.Models.V2.Reservation.ReservationConnector.Request.ExternalReference
                    {
                        TargetReferenceId = "PMS123"
                    }
                }
            };

            _boomiConnector.Setup(x => x.ReservationConnectorCreateAsync(It.IsAny<ReservationConnectorRequest>()))
                           .ReturnsAsync(boomiResp);

            var ex = await Assert.ThrowsAsync<DbUpdateException>(() =>
                _sut.CreateReservationAsync(request, CancellationToken.None));

            Assert.IsTrue(ex.Message.Contains("DB error", StringComparison.Ordinal)); // replaces StringAssert.Contains
        }

        [TestMethod]
        public async Task CreateReservationAsyncWhenProfileCreationThrowsThrows()
        {
            var request = JsonSerializer.Deserialize<ReservationRequest>(SAMPLE_RESERVATION_REQUEST_JSON);
            Assert.IsNotNull(request);

            _reservationProfileRepo.Setup(r => r.CreateProfileAsync(It.IsAny<Profile>()))
                .Throws(new InvalidOperationException("Profile error"));

            var boomiResp = new ReservationConnectorResponse
            {
                Data = new ReservationConnectorResponseData
                {
                    BookingId = "BOOK-123",
                    ExternalReference = new OneExchangeOrderService.Domain.Models.V2.Reservation.ReservationConnector.Request.ExternalReference
                    {
                        TargetReferenceId = "PMS123"
                    }
                }
            };
            _boomiConnector.Setup(x => x.ReservationConnectorCreateAsync(It.IsAny<ReservationConnectorRequest>()))
                .ReturnsAsync(boomiResp);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.CreateReservationAsync(request, CancellationToken.None));
        }

        [TestMethod]
        public async Task CreateReservationAsyncWhenOrderCreationThrowsThrows()
        {
            var request = JsonSerializer.Deserialize<ReservationRequest>(SAMPLE_RESERVATION_REQUEST_JSON);
            Assert.IsNotNull(request);

            _reservationProfileRepo.Setup(r => r.CreateProfileAsync(It.IsAny<Profile>()));
            _reservationOrderRepo.Setup(r => r.CreateOrderAsync(It.IsAny<Order>()))
                .ThrowsAsync(new InvalidOperationException("Order error"));

            var boomiResp = new ReservationConnectorResponse
            {
                Data = new ReservationConnectorResponseData
                {
                    BookingId = "BOOK-123",
                    ExternalReference = new OneExchangeOrderService.Domain.Models.V2.Reservation.ReservationConnector.Request.ExternalReference
                    {
                        TargetReferenceId = "PMS123"
                    }
                }
            };
            _boomiConnector.Setup(x => x.ReservationConnectorCreateAsync(It.IsAny<ReservationConnectorRequest>()))
                .ReturnsAsync(boomiResp);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.CreateReservationAsync(request, CancellationToken.None));
        }

        // ------------------- CANCELLATION TESTS -------------------

        [TestMethod]
        public async Task CancelReservationAsyncHappyPathReturnsCancelled()
        {
            var bookingId = "BOOK-123";
            var request = JsonSerializer.Deserialize<CancellationRequest>(SAMPLE_CANCELLATION_REQUEST_JSON);

            var order = new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD123",
                OrderStatus = "CONFIRMED",
                ExternalReferenceId = bookingId,
                TenantId = Guid.NewGuid(),
                Currency = "USD",
                OrderFee = 30m,
                CustomerFirstName = "John",
                CustomerLastName = "Doe",
                OrderItems =
                [
                    new()
                    {
                        ItemCode = "CLA-CDCCE-K1RRR2",
                        OrderItemAttributes =
                        [
                            new() { AttributeType = "CHECK_IN_DATE", AttributeValue = "2025-12-02" },
                            new() { AttributeType = "CHECK_OUT_DATE", AttributeValue = "2025-12-03" }
                        ]
                    }
                ]
            };

            _reservationOrderRepo.Setup(r => r.GetOrderByExternalReferenceIdAsync(bookingId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            _reservationOrderRepo.Setup(r => r.CreateOrderReturnsAsync(It.IsAny<OrderReturn>())).Returns(Task.CompletedTask);

            var boomiResp = new CancellationResponse
            {
                Data = new CancelItemResponse
                {
                    CancellationStatus = "Cancelled"
                }
            };
            _boomiConnector.Setup(x => x.CancellationConnectorCancelAsync(It.IsAny<CancellationRequest>()))
                .ReturnsAsync(boomiResp);

            _inventoryService.Setup(x => x.UpdateInventoryAsync(It.IsAny<InventoryRequest>()))
                .ReturnsAsync(new InventoryResponse { Success = true });

            var result = await _sut.CancelReservationAsync(bookingId, request, CancellationToken.None);

            Assert.IsNotNull(result);
            Assert.AreEqual("CANCELLED", result.Data.SystemMeta.StatusReason);
            _reservationUow.Verify(x => x.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task CancelReservationAsyncWhenOrderNotFoundThrowsDataNotFoundException()
        {
            var bookingId = "BOOK-404";

            var request = JsonSerializer.Deserialize<CancellationRequest>(SAMPLE_CANCELLATION_REQUEST_JSON)!;

            _reservationOrderRepo
                .Setup(r => r.GetOrderByExternalReferenceIdAsync(
                    bookingId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Order?)null);

            await Assert.ThrowsAsync<DataNotFoundException>(() =>
                _sut.CancelReservationAsync(bookingId, request, CancellationToken.None));
        }

        [TestMethod]
        public async Task CancelReservationAsyncWhenBoomiFailsReturnsFailure()
        {
            var bookingId = "BOOK-123";
            var request = JsonSerializer.Deserialize<CancellationRequest>(SAMPLE_CANCELLATION_REQUEST_JSON);

            var order = new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD123",
                OrderStatus = "CONFIRMED",
                ExternalReferenceId = bookingId
            };

            _reservationOrderRepo.Setup(r => r.GetOrderByExternalReferenceIdAsync(bookingId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            _boomiConnector.Setup(x => x.CancellationConnectorCancelAsync(It.IsAny<CancellationRequest>()))
                .ThrowsAsync(new HttpRequestException("Boomi error"));

            var result = await _sut.CancelReservationAsync(bookingId, request, CancellationToken.None);

            Assert.AreEqual("FAILED", result.Data.CancellationStatus);
            Assert.AreEqual("Unknown error code from Boomi connector", result.Data.Error.ErrorCode);
        }

        [TestMethod]
        public async Task CancelReservationAsyncWhenBoomiReturnsErrorReturnsFailure()
        {
            var bookingId = "BOOK-123";

            var request = JsonSerializer.Deserialize<CancellationRequest>(SAMPLE_CANCELLATION_REQUEST_JSON);
            Assert.IsNotNull(request);

            var order = new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD123",
                OrderStatus = "CONFIRMED",
                ExternalReferenceId = bookingId
            };

            _reservationOrderRepo
                .Setup(r => r.GetOrderByExternalReferenceIdAsync(bookingId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            var boomiResp = new CancellationResponse
            {
                Data = new CancelItemResponse
                {
                    CancellationStatus = "Failed",
                    Error = new ErrorInfo
                    {
                        ErrorCode = "BOOMI_ERR",
                        ErrorMessage = "Boomi error"
                    }
                }
            };

            _boomiConnector
                .Setup(x => x.CancellationConnectorCancelAsync(It.IsAny<CancellationRequest>()))
                .ReturnsAsync(boomiResp);

            var result = await _sut.CancelReservationAsync(bookingId, request, CancellationToken.None);

            Assert.IsNotNull(result.Data);
            Assert.IsNotNull(result.Data.Error);

            Assert.AreEqual("FAILED", result.Data.CancellationStatus);
            Assert.AreEqual("BOOMI_ERR", result.Data.Error.ErrorCode);
        }

        [TestMethod]
        public async Task CancelReservationAsyncWhenInventoryReleaseFailsReturnsFailure()
        {
            var bookingId = "BOOK-123";
            var request = JsonSerializer.Deserialize<CancellationRequest>(SAMPLE_CANCELLATION_REQUEST_JSON);

            var order = new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD123",
                OrderStatus = "CONFIRMED",
                ExternalReferenceId = bookingId,
                OrderItems =
                [
                    new()
                    {
                        ItemCode = "CLA-CDCCE-K1RRR2",
                        OrderItemAttributes =
                        [
                            new() { AttributeType = "CHECK_IN_DATE", AttributeValue = "2025-12-02" },
                            new() { AttributeType = "CHECK_OUT_DATE", AttributeValue = "2025-12-03" }
                        ]
                    }
                ]
            };

            _reservationOrderRepo.Setup(r => r.GetOrderByExternalReferenceIdAsync(bookingId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            var boomiResp = new CancellationResponse
            {
                Data = new CancelItemResponse
                {
                    CancellationStatus = "Cancelled"
                }
            };
            _boomiConnector.Setup(x => x.CancellationConnectorCancelAsync(It.IsAny<CancellationRequest>()))
                .ReturnsAsync(boomiResp);

            _inventoryService.Setup(x => x.UpdateInventoryAsync(It.IsAny<InventoryRequest>()))
                .ReturnsAsync(new InventoryResponse { Success = false, Message = "Inventory release error" });

            var result = await _sut.CancelReservationAsync(bookingId, request, CancellationToken.None);

            Assert.AreEqual("FAILED", result.Data.CancellationStatus);
            Assert.AreEqual("INVENTORY_RELEASE_FAILED", result.Data.Error.ErrorCode);
        }

        [TestMethod]
        public async Task CancelReservationAsyncWhenInventoryUpdateReturnsNullReturnsFailure()
        {
            var bookingId = "BOOK-123";
            var request = JsonSerializer.Deserialize<CancellationRequest>(SAMPLE_CANCELLATION_REQUEST_JSON);
            Assert.IsNotNull(request);

            var order = new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD123",
                OrderStatus = "CONFIRMED",
                ExternalReferenceId = bookingId,
                OrderItems =
                [
                    new()
                    {
                        ItemCode = "CLA-CDCCE-K1RRR2",
                        OrderItemAttributes =
                        [
                            new() { AttributeType = "CHECK_IN_DATE", AttributeValue = "2025-12-02" },
                            new() { AttributeType = "CHECK_OUT_DATE", AttributeValue = "2025-12-03" }
                        ]
                    }
                ]
            };

            _reservationOrderRepo.Setup(r => r.GetOrderByExternalReferenceIdAsync(bookingId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            var boomiResp = new CancellationResponse
            {
                Data = new CancelItemResponse
                {
                    CancellationStatus = "Cancelled"
                }
            };
            _boomiConnector.Setup(x => x.CancellationConnectorCancelAsync(It.IsAny<CancellationRequest>()))
                .ReturnsAsync(boomiResp);

            _inventoryService.Setup(x => x.UpdateInventoryAsync(It.IsAny<InventoryRequest>()))
                .ReturnsAsync((InventoryResponse)null);

            var result = await _sut.CancelReservationAsync(bookingId, request, CancellationToken.None);

            Assert.AreEqual("FAILED", result.Data.CancellationStatus);
            Assert.AreEqual("INVENTORY_RELEASE_FAILED", result.Data.Error.ErrorCode);
        }

        [TestMethod]
        public async Task CancelReservationAsyncWhenSaveChangesThrowsThrows()
        {
            var bookingId = "BOOK-123";
            var request = JsonSerializer.Deserialize<CancellationRequest>(SAMPLE_CANCELLATION_REQUEST_JSON);
            Assert.IsNotNull(request);

            var order = new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD123",
                OrderStatus = "CONFIRMED",
                ExternalReferenceId = bookingId,
                TenantId = Guid.NewGuid(),
                Currency = "USD",
                OrderFee = 30m,
                OrderItems =
                [
                    new()
                    {
                        ItemCode = "CLA-CDCCE-K1RRR2",
                        OrderItemAttributes =
                        [
                            new() { AttributeType = "CHECK_IN_DATE", AttributeValue = "2025-12-02" },
                            new() { AttributeType = "CHECK_OUT_DATE", AttributeValue = "2025-12-03" }
                        ]
                    }
                ]
            };

            _reservationOrderRepo.Setup(r => r.GetOrderByExternalReferenceIdAsync(bookingId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            _reservationOrderRepo.Setup(r => r.CreateOrderReturnsAsync(It.IsAny<OrderReturn>())).Returns(Task.CompletedTask);

            var boomiResp = new CancellationResponse
            {
                Data = new CancelItemResponse
                {
                    CancellationStatus = "Cancelled"
                }
            };
            _boomiConnector.Setup(x => x.CancellationConnectorCancelAsync(It.IsAny<CancellationRequest>()))
                .ReturnsAsync(boomiResp);

            _inventoryService.Setup(x => x.UpdateInventoryAsync(It.IsAny<InventoryRequest>()))
                .ReturnsAsync(new InventoryResponse { Success = true });

            _reservationUow.Setup(x => x.SaveAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new DbUpdateException("DB error"));

            var ex = await Assert.ThrowsAsync<DbUpdateException>(() =>
                _sut.CancelReservationAsync(bookingId, request, CancellationToken.None));

            Assert.Contains(ex.Message, "DB error");
        }

        [TestMethod]
        public async Task CancelReservationAsyncWhenOrderReturnCreationThrowsThrows()
        {
            var bookingId = "BOOK-123";
            var request = JsonSerializer.Deserialize<CancellationRequest>(SAMPLE_CANCELLATION_REQUEST_JSON);
            Assert.IsNotNull(request);

            var order = new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD123",
                OrderStatus = "CONFIRMED",
                ExternalReferenceId = bookingId,
                TenantId = Guid.NewGuid(),
                Currency = "USD",
                OrderFee = 30m,
                OrderItems =
                [
                    new()
                    {
                        ItemCode = "CLA-CDCCE-K1RRR2",
                        OrderItemAttributes =
                        [
                            new() { AttributeType = "CHECK_IN_DATE", AttributeValue = "2025-11-04" },
                            new() { AttributeType = "CHECK_OUT_DATE", AttributeValue = "2025-11-05" }
                        ]
                    }
                ]
            };

            _reservationOrderRepo.Setup(r => r.GetOrderByExternalReferenceIdAsync(bookingId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            _reservationOrderRepo.Setup(r => r.CreateOrderReturnsAsync(It.IsAny<OrderReturn>()))
                .ThrowsAsync(new InvalidOperationException("Return error"));

            var boomiResp = new CancellationResponse
            {
                Data = new CancelItemResponse
                {
                    CancellationStatus = "Cancelled"
                }
            };
            _boomiConnector.Setup(x => x.CancellationConnectorCancelAsync(It.IsAny<CancellationRequest>()))
                .ReturnsAsync(boomiResp);

            _inventoryService.Setup(x => x.UpdateInventoryAsync(It.IsAny<InventoryRequest>()))
                .ReturnsAsync(new InventoryResponse { Success = true });

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.CancelReservationAsync(bookingId, request, CancellationToken.None));
        }

        [TestMethod]
        public async Task CancelReservationAsyncWhenOrderStatusUpdateThrowsThrows()
        {
            var bookingId = "BOOK-123";
            var request = JsonSerializer.Deserialize<CancellationRequest>(SAMPLE_CANCELLATION_REQUEST_JSON);
            Assert.IsNotNull(request);

            var order = new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD123",
                OrderStatus = "CONFIRMED",
                ExternalReferenceId = bookingId,
                TenantId = Guid.NewGuid(),
                Currency = "USD",
                OrderFee = 30m,
                OrderItems =
                [
                    new()
                    {
                        ItemCode = "CLA-CDCCE-K1RRR2",
                        OrderItemAttributes =
                        [
                            new() { AttributeType = "CHECK_IN_DATE", AttributeValue = "2025-12-02" },
                            new() { AttributeType = "CHECK_OUT_DATE", AttributeValue = "2025-12-03" }
                        ]
                    }
                ]
            };

            _reservationOrderRepo.Setup(r => r.GetOrderByExternalReferenceIdAsync(bookingId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            _reservationOrderRepo.Setup(r => r.CreateOrderReturnsAsync(It.IsAny<OrderReturn>())).Returns(Task.CompletedTask);

            var boomiResp = new CancellationResponse
            {
                Data = new CancelItemResponse
                {
                    CancellationStatus = "Cancelled"
                }
            };
            _boomiConnector.Setup(x => x.CancellationConnectorCancelAsync(It.IsAny<CancellationRequest>()))
                .ReturnsAsync(boomiResp);

            _inventoryService.Setup(x => x.UpdateInventoryAsync(It.IsAny<InventoryRequest>()))
                .ReturnsAsync(new InventoryResponse { Success = true });

            _reservationUow.Setup(x => x.SaveAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Status error"));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.CancelReservationAsync(bookingId, request, CancellationToken.None));
        }

        // --- Sample JSONs for deserialization ---
        private const string SAMPLE_RESERVATION_REQUEST_JSON = /*lang=json,strict*/ @"
{
  ""id"": ""1011983242692146475"",
  ""source"": ""OneXChange"",
  ""specversion"": ""1.0"",
  ""type"": ""Reservation-Request"",
  ""datacontenttype"": ""application/json"",
  ""dataschema"": ""https://schemas.onetruth.example/reservations/request/2019-09"",
  ""subject"": ""create new reservation"",
  ""time"": ""2026-02-03T12:50:30.8484295+00:00"",
  ""ispartial"": false,
  ""data"": {
    ""systemMeta"": {
      ""tenantID"": ""HGV"",
      ""_insertedDateTime"": ""2026-02-03T12:50:30.8484298+00:00"",
      ""_insertedBy"": ""api"",
      ""_updatedDateTime"": ""2026-02-03T12:50:30.8484299+00:00"",
      ""_updatedBy"": ""api"",
      ""status"": true,
      ""statusReason"": ""Initiated""
    },
    ""externalReference"": {
      ""sourceSystem"": ""PARTNER"",
      ""targetSystem"": ""HGV"",
      ""sourceReferenceId"": ""RES323322"",
      ""targetReferenceId"": ""HGV""
    },
    ""bookingItems"": [
      {
        ""itemCode"": ""CLA-BWS-24713"",
        ""itemClass"": ""UnitType"",
        ""parentCode"": ""CLA-BWS"",
        ""name"": ""Deluxe oceanfront suite with balcony"",
        ""description"": ""Deluxe oceanfront suite with balcony"",
        ""itemAttributes"": [
          {
            ""attribute"": ""propertyCategory"",
            ""value"": ""5-STAR""
          },
          {
            ""attribute"": ""segment"",
            ""value"": ""LEISURE""
          },
          {
            ""attribute"": ""NS"",
            ""value"": ""Non-smoking room""
          },
          {
            ""attribute"": """",
            ""value"": ""Late check-out at 2 PM""
          }
        ],
        ""memberID"": ""733760277"",
        ""startDate"": ""2026-02-15T00:00:00+00:00"",
        ""endDate"": ""2026-02-16T00:00:00+00:00"",
        ""weekNumber"": 1,
        ""action"": ""Initiate"",
        ""guestCounts"": {
          ""adults"": 1,
          ""children"": 1
        },
        ""isOwnerBooking"": true,
        ""guestInfo"": {
          ""firstName"": ""lalit"",
          ""lastName"": ""Doe"",
          ""postalAddresses"": [
            {
              ""_sequence"": 1,
              ""addressType"": ""business"",
              ""line1"": ""123 Main St"",
              ""line2"": ""Suite 100"",
              ""city"": ""New York"",
              ""state"": ""NY"",
              ""postalCode"": ""10001"",
              ""country"": ""United States""
            }
          ],
          ""phoneNumbers"": [],
          ""digitalAddresses"": []
        },
        ""priceLevel"": {
          ""status"": true,
          ""priceLevel"": ""VIPRATE"",
          ""currency"": ""USD"",
          ""totalPrice"": 162.36,
          ""priceAttributes"": [
            {
              ""attribute"": ""amountBeforeTax"",
              ""value"": ""114""
            },
            {
              ""attribute"": ""taxesTotal"",
              ""value"": ""18.36""
            },
            {
              ""attribute"": ""feesTotal"",
              ""value"": ""30""
            },
			{
			  ""attribute"": ""toCurrencyCode"",
			  ""value"": ""CAD""
			  },
			  {
				""attribute"": ""conversionRate"",
				""value"": ""1.37""
			  },
			  {
			   ""attribute"": ""conversionDate"",
				""value"": ""2026-02-03""
			  }
          ]
        },
        ""bookingAttributes"": [
          {
            ""attribute"": ""guaranteeType"",
            ""value"": ""CREDIT_CARD""
          },
          {
            ""attribute"": ""guaranteeDeadline"",
            ""value"": ""2025-10-31T23:59:59Z""
          },
          {
            ""attribute"": ""CertificateNumber"",
            ""value"": ""CERT2411""
          },
          {
            ""attribute"": ""ProgramName"",
            ""value"": ""HONORS""
          },
          {
            ""attribute"": ""MembershipId"",
            ""value"": ""99876353""
          },
          {
            ""attribute"": ""PromotionCode"",
            ""value"": ""ELITE2026""
          },
          {
            ""attribute"": ""RedemptionQuantity"",
            ""value"": ""25""
          },
          {
            ""attribute"": ""CurrencyCode"",
            ""value"": ""USD""
          }
        ],
        ""bookingComments"": [
          {
            ""attribute"": ""TIMING"",
            ""value"": ""Need wheel chair""
          },
          {
            ""attribute"": ""SPECIALREQUEST"",
            ""value"": ""Non-smoking room""
          },
          {
            ""attribute"": ""SPECIALREQUEST"",
            ""value"": ""Late check-out at 2 PM""
          }
        ]
      }
    ]
  }
}
";
        private const string SAMPLE_CANCELLATION_REQUEST_JSON = /*lang=json,strict*/ @"
{
    ""id"": ""1234"",
    ""source"": ""booking-system"",
    ""specversion"": ""1.0"",
    ""type"": ""booking.cancelled"",
    ""datacontenttype"": ""application/json"",
    ""dataschema"": ""https://example.com/schemas/booking-cancellation"",
    ""subject"": ""booking/evt-12345"",
    ""time"": ""2025-05-29T12:34:56Z"",
    ""ispartial"": false,
    ""data"": {
        ""systemMeta"": {
            ""tenantID"": ""HGV"",
            ""_insertedDateTime"": ""2025-05-28T10:00:00Z"",
            ""_insertedBy"": ""system"",
            ""_updatedDateTime"": ""2025-05-29T11:30:00Z"",
            ""_updatedBy"": ""admin"",
            ""status"": true,
            ""statusReason"": ""Completed""
        },
        ""externalReference"": {
            ""sourceSystem"": ""PARTNER"",
            ""targetSystem"": ""CLA"",
            ""sourceReferenceId"": ""CLA"",
            ""targetReferenceId"": ""CLACancel""
        },
        ""cancellationReason"": ""Customer request due to personal reasons."",
        ""cancellationDate"": ""2025-05-29T11:45:00Z"",
        ""cancellationStatus"": ""cancelled"",
        ""bookingItems"": [
            {
                ""guestInfo"": {
                    ""firstName"": ""John"",
                    ""lastName"": ""Martin""
                }
            }
        ]
    }
}
";
    }
}