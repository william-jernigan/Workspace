using APITransformationsFunction.Functions.Reservation;
using APITransformationsFunction.Model.Cancellation;
using APITransformationsFunction.Model.Reservation.Create.Request;
using APITransformationsFunction.Model.Shared;
using APITransformationsFunction.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Text;
using System.Text.Json;

namespace APITransformationsFunctionTests.FunctionTests
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
        public async Task Returns_406_When_ContentType_Is_Not_Json()
        {
            var req = new FakeHttpRequestData(_context);
            req.Headers.Add("Content-Type", "text/plain");

            var response = await _function.CreateReservation(req);

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns_400_When_Invalid_Json()
        {
            var req = JsonRequest("{ invalid json }");

            var response = await _function.CreateReservation(req);

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns_400_When_Request_Is_Null()
        {
            var req = JsonRequest("null");

            var response = await _function.CreateReservation(req);

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns_200_When_Service_Returns_Success()
        {
            _service.Setup(s =>
                s.CreateReservationAsync(It.IsAny<OTACreateReservationRequest>()))
                .ReturnsAsync(SuccessResponse());

            var req = JsonRequest(ValidRequest());

            var response = await _function.CreateReservation(req);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns_500_When_Exception_Is_Thrown()
        {
            _service.Setup(s =>
                s.CreateReservationAsync(It.IsAny<OTACreateReservationRequest>()))
                .ThrowsAsync(new Exception("boom"));

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
