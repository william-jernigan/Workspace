using APITransformationsFunction.Functions.Member;
using APITransformationsFunction.Model.Cancellation;
using APITransformationsFunction.Model.Member;
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
    public class MemberPostFunctionTests
    {
        private Mock<IMemberService> _service;
        private Mock<ILogger<MemberPostFunction>> _logger;
        private MemberPostFunction _function;
        private FunctionContext _context;

        [TestInitialize]
        public void Setup()
        {
            _service = new Mock<IMemberService>(MockBehavior.Strict);
            _logger = new Mock<ILogger<MemberPostFunction>>();
            _context = new Mock<FunctionContext>().Object;

            _function = new MemberPostFunction(
                _service.Object,
                _logger.Object
            );
        }

        [TestMethod]
        public async Task Returns_406_When_ContentType_Is_Not_Json()
        {
            var req = new FakeHttpRequestData(_context);
            req.Headers.Add("Content-Type", "text/plain");

            var response = await _function.CreateMember(req);

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns_400_When_Invalid_Json()
        {
            var req = JsonRequest("{ invalid json }");

            var response = await _function.CreateMember(req);

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns_400_When_Request_Is_Null()
        {
            var req = JsonRequest("null");

            var response = await _function.CreateMember(req);

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns_Error_Status_When_Service_Returns_Failure()
        {
            _service.Setup(s =>
                s.CreateMemberAsync(It.IsAny<OTAPostMemberRequest>()))
                .ReturnsAsync(new CommonResponseDto<OTAPostMemberResponse>
                {
                    Success = false,
                    Error = new APITransformationsFunction.Model.Shared.OTAErrorResponse
                    {
                        Status = 409,
                        Title = "Conflict"
                    }
                });

            var req = JsonRequest(ValidRequest());

            var response = await _function.CreateMember(req);

            Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns_200_When_Service_Returns_Success()
        {
            _service.Setup(s =>
                s.CreateMemberAsync(It.IsAny<OTAPostMemberRequest>()))
                .ReturnsAsync(SuccessResponse());

            var req = JsonRequest(ValidRequest());

            var response = await _function.CreateMember(req);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns_500_When_Exception_Is_Thrown()
        {
            _service.Setup(s =>
                s.CreateMemberAsync(It.IsAny<OTAPostMemberRequest>()))
                .ThrowsAsync(new Exception("boom"));

            var req = JsonRequest(ValidRequest());

            var response = await _function.CreateMember(req);

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

        private static OTAPostMemberRequest ValidRequest() =>
            new()
            {
                CorrelationID = "corr-1",
                TradingAgreementCode = "HGV",
                MemberProfile = new MemberProfile
                {
                    FirstName = "John",
                    LastName = "Doe"
                },
                ContactInfo = new ContactInfo(),
                LoyaltyPrograms = []
            };

        private static CommonResponseDto<OTAPostMemberResponse> SuccessResponse() =>
            new()
            {
                Success = true,
                Data = new OTAPostMemberResponse
                {
                    CorrelationID = "corr-1",
                    TradingAgreementCode = "HGV",
                    MemberStatus = "ACTIVE",
                    MemberProfile = new OTAMemberProfile()
                }
            };
    }
}
