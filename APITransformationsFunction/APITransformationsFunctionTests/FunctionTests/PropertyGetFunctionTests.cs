using APITransformationsFunction.Functions.Property;
using APITransformationsFunction.Model.Cancellation;
using APITransformationsFunction.Model.Property;
using APITransformationsFunction.Model.Shared;
using APITransformationsFunction.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;

namespace APITransformationsFunctionTests.FunctionTests
{
    [TestClass]
    public class PropertyGetFunctionTests
    {
        private Mock<IPropertyService> _service;
        private Mock<ILogger<PropertyGetFunction>> _logger;
        private PropertyGetFunction _function;
        private FunctionContext _context;

        [TestInitialize]
        public void Setup()
        {
            _service = new Mock<IPropertyService>(MockBehavior.Strict);
            _logger = new Mock<ILogger<PropertyGetFunction>>();
            _context = new Mock<FunctionContext>().Object;

            _function = new PropertyGetFunction(
                _service.Object,
                _logger.Object
            );
        }

        [TestMethod]
        public async Task Returns_400_When_PropertyCode_Is_Empty()
        {
            var req = new FakeHttpRequestData(_context);
            req.SetMethod("GET");

            var response = await _function.GetPropertyByCode(req, "");

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns_404_When_Service_Returns_Null()
        {
            _service.Setup(s =>
                s.GetPropertyByCodeAsync("PROP1", "NONE"))
                .ReturnsAsync((CommonResponseDto<OTAPropertySearchResponse>)null!);

            var req = new FakeHttpRequestData(_context);
            req.SetMethod("GET");

            var response = await _function.GetPropertyByCode(req, "PROP1");

            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns_200_When_Single_Property_Is_Returned()
        {
            _service.Setup(s =>
                s.GetPropertyByCodeAsync("PROP1", "NONE"))
                .ReturnsAsync(SuccessResponse(singleProperty: true));

            var req = new FakeHttpRequestData(_context);
            req.SetMethod("GET");

            var response = await _function.GetPropertyByCode(req, "PROP1");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns_200_When_Multiple_Properties_Are_Returned()
        {
            _service.Setup(s =>
                s.GetPropertyByCodeAsync("PROP1", "NONE"))
                .ReturnsAsync(SuccessResponse(singleProperty: false));

            var req = new FakeHttpRequestData(_context);
            req.SetMethod("GET");

            var response = await _function.GetPropertyByCode(req, "PROP1");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns_Error_Status_From_Service()
        {
            _service.Setup(s =>
                s.GetPropertyByCodeAsync("PROP1", "NONE"))
                .ReturnsAsync(new CommonResponseDto<OTAPropertySearchResponse>
                {
                    Success = false,
                    Error = new APITransformationsFunction.Model.Shared.OTAErrorResponse
                    {
                        Status = 403,
                        Title = "Forbidden"
                    }
                });

            var req = new FakeHttpRequestData(_context);
            req.SetMethod("GET");

            var response = await _function.GetPropertyByCode(req, "PROP1");

            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns_500_When_Exception_Is_Thrown()
        {
            _service.Setup(s =>
                s.GetPropertyByCodeAsync("PROP1", It.IsAny<string>()))
                .ThrowsAsync(new Exception("boom"));

            var req = new FakeHttpRequestData(_context);
            req.SetMethod("GET");

            var response = await _function.GetPropertyByCode(req, "PROP1");

            Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        private static CommonResponseDto<OTAPropertySearchResponse> SuccessResponse(bool singleProperty)
        {
            var properties = singleProperty
                ?
                  [
                      new()
                      {
                          BasicPropertyInfoSummary = new BasicPropertyInfoSummary
                          {
                              PropertyCode = "PROP1",
                              Name = "Test Property"
                          },
                          UnitDetails = []
                      }
                  ]
                : new List<PropertyResponseItem>
                  {
                      new() { BasicPropertyInfoSummary = new BasicPropertyInfoSummary { PropertyCode = "P1" } },
                      new() { BasicPropertyInfoSummary = new BasicPropertyInfoSummary { PropertyCode = "P2" } }
                  };

            return new CommonResponseDto<OTAPropertySearchResponse>
            {
                Success = true,
                Data = new OTAPropertySearchResponse
                {
                    CorrelationID = "corr-1",
                    TradingAgreementCode = "HGV",
                    Properties = properties
                }
            };
        }
    }
}
