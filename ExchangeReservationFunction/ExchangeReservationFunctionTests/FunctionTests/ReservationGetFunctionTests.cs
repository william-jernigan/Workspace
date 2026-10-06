using ExchangeReservationFunction.Functions.Reservation;
using ExchangeReservationFunction.Model.Reservation.GetReservation;
using ExchangeReservationFunction.Model.Common;
using ExchangeReservationFunction.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;

namespace ExchangeReservationFunctionTests.FunctionTests
{
    [TestClass]
    public class ReservationGetFunctionTests
    {
        private Mock<IReservationService>? _service;
        private Mock<ILogger<ReservationGetFunction>>? _logger;
        private ReservationGetFunction? _function;
        private FunctionContext? _context;

        [TestInitialize]
        public void Setup()
        {
            _service = new Mock<IReservationService>(MockBehavior.Strict);
            _logger = new Mock<ILogger<ReservationGetFunction>>();
            _context = new Mock<FunctionContext>().Object;

            _function = new ReservationGetFunction(
                _service.Object,
                _logger.Object
            );
        }

        [TestMethod]
        public async Task Returns400WhenReservationIdIsEmpty()
        {
            var req = new FakeHttpRequestData(_context);
            req.SetMethod("GET");

            var response = await _function.GetReservationById(req, "");

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns404WhenServiceReturnsNull()
        {
            _service.Setup(s =>
                s.GetReservationByIdAsync("RES1"))
                .ReturnsAsync((CommonResponseDto<OTAGetReservationResponse>)null!);

            var req = new FakeHttpRequestData(_context);
            req.SetMethod("GET");

            var response = await _function.GetReservationById(req, "RES1");

            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns200WhenServiceReturnsSuccess()
        {
            _service.Setup(s =>
                s.GetReservationByIdAsync("RES1"))
                .ReturnsAsync(SuccessResponse());

            var req = new FakeHttpRequestData(_context);
            req.SetMethod("GET");

            var response = await _function.GetReservationById(req, "RES1");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public async Task ReturnsErrorStatusFromService()
        {
            _service.Setup(s =>
                s.GetReservationByIdAsync("RES1"))
                .ReturnsAsync(new CommonResponseDto<OTAGetReservationResponse>
                {
                    Success = false,
                    Error = new OTAErrorResponse
                    {
                        Status = 404,
                        Title = "Not Found"
                    }
                });

            var req = new FakeHttpRequestData(_context);
            req.SetMethod("GET");

            var response = await _function.GetReservationById(req, "RES1");

            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns500WhenExceptionIsThrown()
        {
            _service.Setup(s =>
                s.GetReservationByIdAsync("RES1"))
                .ThrowsAsync(new InvalidOperationException());

            var req = new FakeHttpRequestData(_context);
            req.SetMethod("GET");

            var response = await _function.GetReservationById(req, "RES1");

            Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        private static CommonResponseDto<OTAGetReservationResponse> SuccessResponse() =>
            new()
            {
                Success = true,
                Data = new OTAGetReservationResponse
                {
                    CorrelationID = "corr-1",
                    TradingAgreementCode = "HGV",
                    Reservations = []
                }
            };
    }
}
