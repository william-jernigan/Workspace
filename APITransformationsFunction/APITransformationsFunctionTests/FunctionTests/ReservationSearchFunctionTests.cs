using APITransformationsFunction.Functions.Reservation;
using APITransformationsFunction.Model.Cancellation;
using APITransformationsFunction.Model.Reservation.Search;
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
    public class ReservationSearchFunctionTests
    {
        private Mock<IReservationService> _service = null!;
        private Mock<ILogger<ReservationGetFunction>> _logger = null!;
        private ReservationSearchFunction _function = null!;
        private FunctionContext _context = null!;

        [TestInitialize]
        public void Setup()
        {
            _service = new Mock<IReservationService>(MockBehavior.Strict);
            _logger = new Mock<ILogger<ReservationGetFunction>>();
            _context = new Mock<FunctionContext>().Object;

            _function = new ReservationSearchFunction(
                _service.Object,
                _logger.Object
            );
        }

        [TestMethod]
        public async Task Returns_406_When_ContentType_Is_Not_Json()
        {
            var req = new FakeHttpRequestData(_context);
            req.Headers.Add("Content-Type", "text/plain");

            var response = await _function.SearchReservation(req);

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns_400_When_Invalid_Json()
        {
            var req = JsonRequest("{ invalid json }");

            var response = await _function.SearchReservation(req);

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns_400_When_Request_Is_Null()
        {
            var req = JsonRequest("null");

            var response = await _function.SearchReservation(req);

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns_404_When_Service_Returns_Null()
        {
            _service.Setup(s =>
                s.SearchReservationAsync(It.IsAny<OTASearchReservationRequest>(), 1, 20))
                .ReturnsAsync((CommonResponseDto<OTASearchReservationResponse>)null!);

            var req = JsonRequest(ValidRequest());

            var response = await _function.SearchReservation(req);

            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns_200_When_Service_Returns_Success()
        {
            _service.Setup(s =>
                s.SearchReservationAsync(It.IsAny<OTASearchReservationRequest>(), 1, 20))
                .ReturnsAsync(SuccessResponse());

            var req = JsonRequest(ValidRequest());

            var response = await _function.SearchReservation(req);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns_500_When_Exception_Is_Thrown()
        {
            _service.Setup(s =>
                s.SearchReservationAsync(It.IsAny<OTASearchReservationRequest>(), It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new Exception("boom"));

            var req = JsonRequest(ValidRequest());

            var response = await _function.SearchReservation(req);

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

        private static OTASearchReservationRequest ValidRequest() =>
            new()
            {
                CorrelationID = "corr-1",
                TradingAgreementCode = "HGV"
            };

        private static CommonResponseDto<OTASearchReservationResponse> SuccessResponse() =>
            new()
            {
                Success = true,
                Data = new OTASearchReservationResponse
                {
                    CorrelationID = "corr-1",
                    TradingAgreementCode = "HGV",
                    Reservations = []
                }
            };
    }

}
