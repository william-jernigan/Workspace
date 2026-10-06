using APITransformationsFunction.Model.Cancellation;
using APITransformationsFunction.Model.Shared;
using APITransformationsFunction.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;

namespace APITransformationsFunctionTests.ServiceTests
{
    [TestClass]
    public class CancellationServiceTests
    {
        private Mock<ILogger<CancellationService>>? _mockLogger;
        private Mock<HttpMessageHandler>? _mockHttpHandler;
        private HttpClient? _httpClient;
        private CancellationService? _cancellationService;

        [TestInitialize]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<CancellationService>>();
            _mockHttpHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            _httpClient = new HttpClient(_mockHttpHandler.Object)
            {
                BaseAddress = new Uri("https://api.hgv.com")
            };
            _cancellationService = new CancellationService(_httpClient, _mockLogger.Object);
        }

        private OTACancelPostRequest GetValidCancelRequest() => new()
        {
            CorrelationID = Guid.NewGuid().ToString(),
            TradingAgreementCode = "HGV-ARV-HGV",
            Reservations = new ReservationDetails
            {
                SourceReservationIds =
            [
                new SourceReservationId { SourceSystem = "PARTNER", Id = "RES12345" }
            ]
            },
            CancellationInfo = new CommonCancellationInfo
            {
                CancelReason = "Customer requested cancellation",
                CancelDate = DateTime.UtcNow,
                CancelComments = "Change in travel plan"
            }
        };

        [TestMethod]
        public async Task CancelReservationAsync_ReturnsSuccess_WhenBackendReturnsValidResponse()
        {
            // Arrange
            var otaRequest = GetValidCancelRequest();
            var reservationId = "RES12345";

            var backendResponse = new ONEXCancelPostResponse
            {
                Id = Guid.NewGuid().ToString(),
                Source = "OneExchange",
                Specversion = "1.0",
                Type = "booking.cancelled",
                Datacontenttype = "application/json",
                Dataschema = "https://schemas.hgv.com/reservations/cancel/2020-12",
                Subject = "booking/evt-12345",
                Time = DateTimeOffset.UtcNow,
                Ispartial = false,
                Data = new CancelItemResponse
                {
                    SystemMeta = new APITransformationsFunction.Model.Cancellation.SystemMeta
                    {
                        TenantID = "HGV",
                        InsertedDateTime = DateTimeOffset.UtcNow,
                        InsertedBy = "System",
                        UpdatedBy = "System",
                        UpdatedDateTime = DateTimeOffset.UtcNow,
                        Status = true,
                        StatusReason = "Cancelled successfully"
                    },
                    CancellationReason = "Customer requested cancellation",
                    CancellationDate = DateTimeOffset.UtcNow,
                    BookingId = reservationId
                }
            };

            var backendJson = JsonSerializer.Serialize(backendResponse, JsonOptionsProvider.Options);

            _mockHttpHandler!
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(backendJson, Encoding.UTF8, "application/json")
                });

            // Act
            var result = await _cancellationService!.CancelReservationAsync(otaRequest, reservationId);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.IsNotNull(result.Data);
            Assert.AreEqual("CANCELLED", result.Data.CancellationInfo.Status, "Expected cancellation status to be 'Cancelled'.");
            _mockHttpHandler!.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }

        [TestMethod]
        public async Task CancelReservationAsync_ReturnsBadRequest_WhenValidationFails()
        {
            // Arrange
            var invalidRequest = new OTACancelPostRequest
            {
                CorrelationID = "invalid-guid", // not UUID
                TradingAgreementCode = "HGV-ARV-HGV",
                Reservations = new ReservationDetails
                {
                    SourceReservationIds =
                [
                    new SourceReservationId { SourceSystem = "TEST", Id = "123" } // invalid SourceSystem
                ]
                },
                CancellationInfo = new CommonCancellationInfo
                {
                    CancelReason = "Invalid data",
                    CancelDate = DateTime.UtcNow,
                    CancelComments = "Invalid"
                }
            };

            // Act
            var result = await _cancellationService!.CancelReservationAsync(invalidRequest, "RES999");

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Error, "result.Error should not be null");
            Assert.AreEqual(400, result.Error.Status);
            Assert.AreEqual("Bad Request", result.Error.Title);
            _mockHttpHandler!.Protected().Verify(
                "SendAsync",
                Times.Never(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }

        [TestMethod]
        public async Task CancelReservationAsync_ReturnsBadRequest_WhenReservationIdIsEmpty()
        {
            // Arrange
            var validRequest = GetValidCancelRequest();

            // Act
            var result = await _cancellationService!.CancelReservationAsync(validRequest, "");

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Error, "result.Error should not be null");
            Assert.AreEqual(400, result.Error.Status);
            _mockHttpHandler!.Protected().Verify(
                "SendAsync",
                Times.Never(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }

        [TestMethod]
        public async Task CancelReservationAsync_ReturnsInternalServerError_WhenBackendThrowsException()
        {
            // Arrange
            var validRequest = GetValidCancelRequest();
            var reservationId = "RES12345";

            _mockHttpHandler!
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Connection failed"));

            // Act
            var result = await _cancellationService!.CancelReservationAsync(validRequest, reservationId);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Error, "result.Error should not be null");
            Assert.AreEqual(500, result.Error.Status);
            StringAssert.Contains(result.Error.Title, "Internal Server Error");
        }

        [TestMethod]
        public async Task CancelReservationAsync_ReturnsFailure_WhenBackendReturnsBadRequest()
        {
            // Arrange
            var validRequest = GetValidCancelRequest();
            var reservationId = "RES12345";

            _mockHttpHandler!
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("Invalid data", Encoding.UTF8, "application/json")
                });

            // Act
            var result = await _cancellationService!.CancelReservationAsync(validRequest, reservationId);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Error, "result.Error should not be null");
            Assert.AreEqual(400, result.Error.Status);
            Assert.AreEqual("Bad Request", result.Error.Title);
        }

        [TestMethod]
        public async Task CancelReservationAsync_ReturnsBadRequest_WhenReservationIdIsWhitespace()
        {
            var result = await _cancellationService.CancelReservationAsync(
                GetValidCancelRequest(), "   ");

            Assert.IsFalse(result.Success);
            Assert.AreEqual(400, result.Error.Status);
        }

        [TestMethod]
        public async Task CancelReservationAsync_ReturnsBadRequest_WhenTradingAgreementCodeTooLong()
        {
            var request = GetValidCancelRequest();
            request.TradingAgreementCode = new string('A', 21);

            var result = await _cancellationService.CancelReservationAsync(request, "RES123");

            Assert.IsFalse(result.Success);
            Assert.AreEqual(400, result.Error.Status);
        }

        [TestMethod]
        public async Task CancelReservationAsync_ReturnsBadRequest_WhenSourceReservationIdsMissing()
        {
            var request = GetValidCancelRequest();
            request.Reservations.SourceReservationIds = [];

            var result = await _cancellationService.CancelReservationAsync(request, "RES123");

            Assert.IsFalse(result.Success);
            Assert.AreEqual(400, result.Error.Status);
        }

        [TestMethod]
        public async Task CancelReservationAsync_AllowsLowercaseSourceSystem()
        {
            var request = GetValidCancelRequest();
            request.Reservations.SourceReservationIds[0].SourceSystem = "partner";

            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                });

            var result = await _cancellationService.CancelReservationAsync(request, "RES123");

            Assert.IsTrue(result.Success);
        }
        [TestMethod]
        public async Task CancelReservationAsync_ReturnsBadRequest_WhenCancelReasonTooLong()
        {
            var request = GetValidCancelRequest();
            request.CancellationInfo.CancelReason = new string('A', 501);

            var result = await _cancellationService.CancelReservationAsync(request, "RES123");

            Assert.IsFalse(result.Success);
            Assert.AreEqual(400, result.Error.Status);
        }
        [TestMethod]
        public async Task CancelReservationAsync_ReturnsBadRequest_WhenCancelCommentsTooLong()
        {
            var request = GetValidCancelRequest();
            request.CancellationInfo.CancelComments = new string('A', 501);

            var result = await _cancellationService.CancelReservationAsync(request, "RES123");

            Assert.IsFalse(result.Success);
            Assert.AreEqual(400, result.Error.Status);
        }
        [TestMethod]
        public async Task CancelReservationAsync_ReturnsBadRequest_WhenCancelDateIsDefault()
        {
            var request = GetValidCancelRequest();
            request.CancellationInfo.CancelDate = default;

            var result = await _cancellationService.CancelReservationAsync(request, "RES123");

            Assert.IsFalse(result.Success);
            Assert.AreEqual(400, result.Error.Status);
        }

        [TestMethod]
        public async Task CancelReservationAsync_ReturnsSuccess_WhenBackendReturnsEmptyJsonObject()
        {
            var request = GetValidCancelRequest();

            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                });

            var result = await _cancellationService.CancelReservationAsync(request, "RES123");

            Assert.IsTrue(result.Success);
            Assert.IsNotNull(result.Data);
        }

        [TestMethod]
        public async Task CancelReservationAsync_ReturnsBadRequest_WhenReservationsIsNull()
        {
            var request = GetValidCancelRequest();
            request.Reservations = null;

            var result = await _cancellationService.CancelReservationAsync(request, "RES123");

            Assert.IsFalse(result.Success);
            Assert.AreEqual(400, result.Error.Status);
        }

        [TestMethod]
        public async Task CancelReservationAsync_ReturnsBadRequest_WhenSourceReservationIdIsNull()
        {
            var request = GetValidCancelRequest();
            request.Reservations.SourceReservationIds[0].Id = null;

            var result = await _cancellationService.CancelReservationAsync(request, "RES123");

            Assert.IsFalse(result.Success);
            Assert.AreEqual(400, result.Error.Status);
        }

        [TestMethod]
        public async Task CancelReservationAsync_ReturnsBadRequest_WhenTradingAgreementCodeIsNull()
        {
            var request = GetValidCancelRequest();
            request.TradingAgreementCode = null;

            var result = await _cancellationService.CancelReservationAsync(request, "RES123");

            Assert.IsFalse(result.Success);
            Assert.AreEqual(400, result.Error.Status);
        }

        [TestMethod]
        public async Task CancelReservationAsync_ReturnsBadRequest_WhenBackendReturnsNullLiteral()
        {
            var request = GetValidCancelRequest();

            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                });

            var result = await _cancellationService.CancelReservationAsync(request, "RES123");

            Assert.IsFalse(result.Success);
            Assert.AreEqual(400, result.Error.Status);
            Assert.AreEqual("Bad Request", result.Error.Title);
        }

        [TestMethod]
        public async Task CancelReservationAsync_ReturnsBadRequest_WhenCorrelationIdLengthIsInvalid()
        {
            var request = GetValidCancelRequest();
            request.CorrelationID = "12345";

            var result = await _cancellationService.CancelReservationAsync(request, "RES123");

            Assert.IsFalse(result.Success);
            Assert.AreEqual(400, result.Error.Status);
        }

        [TestMethod]
        public async Task CancelReservationAsync_ReturnsBadRequest_WhenSourceSystemIsNull()
        {
            var request = GetValidCancelRequest();
            request.Reservations.SourceReservationIds[0].SourceSystem = null;

            var result = await _cancellationService.CancelReservationAsync(request, "RES123");

            Assert.IsFalse(result.Success);
            Assert.AreEqual(400, result.Error.Status);
        }
        [TestMethod]
        public async Task CancelReservationAsync_ReturnsBadRequest_WhenCancelReasonIsNull()
        {
            var request = GetValidCancelRequest();
            request.CancellationInfo.CancelReason = null;

            var result = await _cancellationService.CancelReservationAsync(request, "RES123");

            Assert.IsFalse(result.Success);
            Assert.AreEqual(400, result.Error.Status);
        }

        [TestMethod]
        public async Task CancelReservationAsync_ReturnsBadRequest_WhenCancelCommentsIsNull()
        {
            var request = GetValidCancelRequest();
            request.CancellationInfo.CancelComments = null;

            var result = await _cancellationService.CancelReservationAsync(request, "RES123");

            Assert.IsFalse(result.Success);
            Assert.AreEqual(400, result.Error.Status);
        }

        [TestMethod]
        public async Task CancelReservationAsync_ReturnsBadRequest_WhenCancellationInfoIsNull()
        {
            var request = GetValidCancelRequest();
            request.CancellationInfo = null;

            var result = await _cancellationService.CancelReservationAsync(request, "RES123");

            Assert.IsFalse(result.Success);
            Assert.AreEqual(400, result.Error.Status);
        }
    }
}
