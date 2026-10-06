using APITransformationsFunction.Functions.Reservation;
using APITransformationsFunction.Model.Cancellation;
using APITransformationsFunction.Model.Reservation.Get;
using APITransformationsFunction.Model.Shared;
using APITransformationsFunction.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;

namespace APITransformationsFunctionTests.FunctionTests
{
    [TestClass]
    public class ReservationGetFunctionTests
    {
        private Mock<IReservationService> _service;
        private Mock<ILogger<ReservationGetFunction>> _logger;
        private ReservationGetFunction _function;
        private FunctionContext _context;

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
        public async Task Returns_400_When_ReservationId_Is_Empty()
        {
            var req = new FakeHttpRequestData(_context);
            req.SetMethod("GET");

            var response = await _function.GetReservationById(req, "");

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns_404_When_Service_Returns_Null()
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
        public async Task Returns_200_When_Service_Returns_Success()
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
        public async Task Returns_Error_Status_From_Service()
        {
            _service.Setup(s =>
                s.GetReservationByIdAsync("RES1"))
                .ReturnsAsync(new CommonResponseDto<OTAGetReservationResponse>
                {
                    Success = false,
                    Error = new APITransformationsFunction.Model.Shared.OTAErrorResponse
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
        public async Task Returns_500_When_Exception_Is_Thrown()
        {
            _service.Setup(s =>
                s.GetReservationByIdAsync("RES1"))
                .ThrowsAsync(new Exception("boom"));

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
