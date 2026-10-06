using APITransformationsFunction.Functions.Property;
using APITransformationsFunction.Model.Cancellation;
using APITransformationsFunction.Model.Property;
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
    public class PropertyPostFunctionTests
    {
        private Mock<IPropertyService> _service;
        private Mock<ILogger<PropertyPostFunction>> _logger;
        private PropertyPostFunction _function;
        private FunctionContext _context;

        [TestInitialize]
        public void Setup()
        {
            _service = new Mock<IPropertyService>(MockBehavior.Strict);
            _logger = new Mock<ILogger<PropertyPostFunction>>();
            _context = new Mock<FunctionContext>().Object;

            _function = new PropertyPostFunction(
                _service.Object,
                _logger.Object
            );
        }

        [TestMethod]
        public async Task Returns_406_When_ContentType_Is_Not_Json()
        {
            var req = new FakeHttpRequestData(_context);
            req.Headers.Add("Content-Type", "text/plain");

            var response = await _function.GetProperties(req);

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns_400_When_Invalid_Json()
        {
            var req = JsonRequest("{ invalid json }");

            var response = await _function.GetProperties(req);

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns_400_When_Request_Is_Null()
        {
            var req = JsonRequest("null");

            var response = await _function.GetProperties(req);

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns_200_When_Service_Returns_Success_DefaultPaging()
        {
            _service.Setup(s =>
                s.GetPropertiesAsync(It.IsAny<OTAPropertySearchRequest>(), 1, 20))
                .ReturnsAsync(SuccessResponse());

            var req = JsonRequest(ValidRequest());

            var response = await _function.GetProperties(req);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns_Error_Status_From_Service()
        {
            _service.Setup(s =>
                s.GetPropertiesAsync(It.IsAny<OTAPropertySearchRequest>(), 1, 20))
                .ReturnsAsync(new CommonResponseDto<OTAPropertySearchResponse>
                {
                    Success = false,
                    Error = new APITransformationsFunction.Model.Shared.OTAErrorResponse
                    {
                        Status = 403,
                        Title = "Forbidden"
                    }
                });

            var req = JsonRequest(ValidRequest());

            var response = await _function.GetProperties(req);

            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns_500_When_Exception_Is_Thrown()
        {
            _service.Setup(s =>
                s.GetPropertiesAsync(It.IsAny<OTAPropertySearchRequest>(), It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new Exception("boom"));

            var req = JsonRequest(ValidRequest());

            var response = await _function.GetProperties(req);

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

        private static OTAPropertySearchRequest ValidRequest() =>
            new()
            {
                CorrelationID = "corr-1",
                TradingAgreementCode = "HGV"
            };

        private static CommonResponseDto<OTAPropertySearchResponse> SuccessResponse() =>
            new()
            {
                Success = true,
                Data = new OTAPropertySearchResponse
                {
                    CorrelationID = "corr-1",
                    TradingAgreementCode = "HGV",
                    Properties = []
                }
            };
    }
}

