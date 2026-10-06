using ExchangeReservationFunction.Functions.Reservation;
using ExchangeReservationFunction.Model.Reservation.Create.Request;
using ExchangeReservationFunction.Model.Common;
using ExchangeReservationFunction.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Text;
using System.Text.Json;

namespace ExchangeReservationFunctionTests.FunctionTests
{
    [TestClass]
    public class ReservationPostFunctionTests
    {
        private Mock<IReservationService> _service = null!;
        private Mock<ILogger<ReservationPostFunction>> _logger = null!;
        private ReservationPostFunction _function = null!;
        private FunctionContext _context = null!;

        [TestInitialize]
        public void Setup()
        {
            _service = new Mock<IReservationService>(MockBehavior.Strict);
            _logger = new Mock<ILogger<ReservationPostFunction>>();
            _context = new Mock<FunctionContext>().Object;

            _function = new ReservationPostFunction(
                _service.Object,
                _logger.Object
            );
        }

        [TestMethod]
        public async Task Returns406WhenContentTypeIsNotJson()
        {
            var req = new FakeHttpRequestData(_context);
            req.Headers.Add("Content-Type", "text/plain");

            var response = await _function.CreateReservation(req);

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns400WhenInvalidJson()
        {
            var req = JsonRequest("{ invalid json }");

            var response = await _function.CreateReservation(req);

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns400WhenRequestIsNull()
        {
            var req = JsonRequest("null");

            var response = await _function.CreateReservation(req);

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns200WhenServiceReturnsSuccess()
        {
            _service.Setup(s =>
                s.CreateReservationAsync(It.IsAny<OTACreateReservationRequest>()))
                .ReturnsAsync(SuccessResponse());

            var req = JsonRequest(ValidRequest());

            var response = await _function.CreateReservation(req);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns500WhenExceptionIsThrown()
        {
            _service.Setup(s =>
                s.CreateReservationAsync(It.IsAny<OTACreateReservationRequest>()))
                .ThrowsAsync(new InvalidOperationException());

            var req = JsonRequest(ValidRequest());

            var response = await _function.CreateReservation(req);

            Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        private FakeHttpRequestData JsonRequest(object body)
        {
            var req = new FakeHttpRequestData(_context);
            req.Headers.Add("Content-Type", "application/json");

            var json = body is string s
                ? s
                : JsonSerializer.Serialize(body, JsonOptionsProvider.Options);

            var bytes = Encoding.UTF8.GetBytes(json);
            req.Body.Write(bytes, 0, bytes.Length);
            req.Body.Position = 0;

            return req;
        }

        private static OTACreateReservationRequest ValidRequest() =>
            new()
            {
                CorrelationID = "corr-1",
                TradingAgreementCode = "HGV",
                ReservationCandidates = new ReservationCandidates
                {
                    Properties = new Properties
                    {
                        BasicPropertyInfoSummary = new BasicPropertyInfoSummaryPostRequest
                        {
                            PropertyCode = "PROP1",
                            UnitStays = []
                        }
                    }
                }
            };

        private static CommonResponseDto<OTACreateReservationResponse> SuccessResponse() =>
            new()
            {
                Success = true,
                Data = new OTACreateReservationResponse
                {
                    CorrelationID = "corr-1",
                    TradingAgreementCode = "HGV",
                    Reservations = []
                }
            };
    }
}
