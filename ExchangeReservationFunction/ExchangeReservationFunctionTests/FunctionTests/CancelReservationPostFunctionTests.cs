using ExchangeReservationFunction.Functions.Cancellation;
using ExchangeReservationFunction.Model.Cancellation;
using ExchangeReservationFunction.Model.Common;
using ExchangeReservationFunction.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Text;
using System.Text.Json;
using FakeHttpRequestData = ExchangeReservationFunction.Model.Cancellation.FakeHttpRequestData;

namespace ExchangeReservationFunctionTests.FunctionTests
{
    [TestClass]
    public class CancelReservationPostFunctionTests
    {
        private Mock<ICancellationService> _service;
        private Mock<ILogger<CancelReservationPostFunction>> _logger;
        private CancelReservationPostFunction _function;
        private FunctionContext _context;

        [TestInitialize]
        public void Setup()
        {
            _service = new Mock<ICancellationService>(MockBehavior.Strict);
            _logger = new Mock<ILogger<CancelReservationPostFunction>>();
            _context = new Mock<FunctionContext>().Object;

            _function = new CancelReservationPostFunction(
                _service.Object,
                _logger.Object
            );
        }

        [TestMethod]
        public async Task Returns406WhenContentTypeIsNotJson()
        {
            var req = new FakeHttpRequestData(_context);
            req.Headers.Add("Content-Type", "text/plain");

            var response = await _function.CancelReservation(req, "RES1");

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns400WhenInvalidJson()
        {
            var req = new FakeHttpRequestData(_context);
            req.Headers.Add("Content-Type", "application/json");
            WriteBody(req, "{ invalid json }");

            var response = await _function.CancelReservation(req, "RES1");

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns400WhenDeserializedRequestIsNull()
        {
            var req = new FakeHttpRequestData(_context);
            req.Headers.Add("Content-Type", "application/json");
            WriteBody(req, "null");

            var response = await _function.CancelReservation(req, "RES1");

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns500WhenExceptionIsThrown()
        {
            _service.Setup(s =>
                s.CancelReservationAsync(It.IsAny<OTACancelPostRequest>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException());

            var req = JsonRequest(ValidRequest());

            var response = await _function.CancelReservation(req, "RES1");

            Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        }
        private static FakeHttpRequestData JsonRequest(object body)
        {
            var ctx = new Mock<FunctionContext>().Object;
            var req = new FakeHttpRequestData(ctx);
            req.Headers.Add("Content-Type", "application/json");
            WriteBody(req, JsonSerializer.Serialize(body, JsonOptionsProvider.Options));
            return req;
        }

        private static void WriteBody(FakeHttpRequestData req, string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            req.Body.Write(bytes, 0, bytes.Length);
            req.Body.Position = 0;
        }

        private static OTACancelPostRequest ValidRequest() =>
            new()
            {
                CorrelationID = "123",
                TradingAgreementCode = "HGV",
                Reservations = new ReservationDetails
                {
                    SourceReservationIds =
                    [
                        new SourceReservationId { SourceSystem = "SYS", Id = "1" }
                    ]
                },
                CancellationInfo = new CommonCancellationInfo
                {
                    CancelReason = "TEST",
                    CancelDate = DateTime.UtcNow
                }
            };
    }
}
