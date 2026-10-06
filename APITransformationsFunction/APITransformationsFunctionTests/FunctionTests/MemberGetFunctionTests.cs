using APITransformationsFunction.Functions.Member;
using APITransformationsFunction.Model.Cancellation;
using APITransformationsFunction.Model.Member;
using APITransformationsFunction.Model.Shared;
using APITransformationsFunction.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;

namespace APITransformationsFunctionTests.FunctionTests
{
    [TestClass]
    public class MemberGetFunctionTests
    {
        private Mock<IMemberService> _service;
        private Mock<ILogger<MemberGetFunction>> _logger;
        private MemberGetFunction _function;
        private FunctionContext _context;

        [TestInitialize]
        public void Setup()
        {
            _service = new Mock<IMemberService>(MockBehavior.Strict);
            _logger = new Mock<ILogger<MemberGetFunction>>();
            _context = new Mock<FunctionContext>().Object;

            _function = new MemberGetFunction(
                _service.Object,
                _logger.Object
            );
        }

        [TestMethod]
        public async Task Returns_400_When_MemberId_Is_Empty()
        {
            var req = new FakeHttpRequestData(_context);
            req.SetMethod("GET");

            var response = await _function.GetMemberById(req, "");

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns_400_When_MemberId_Is_Invalid_Format()
        {
            var req = new FakeHttpRequestData(_context);
            req.SetMethod("GET");

            var response = await _function.GetMemberById(req, "abc-123");

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns_404_When_Service_Returns_Null()
        {
            _service.Setup(s =>
                s.GetMemberByIdAsync("MEMBER1"))
                .ReturnsAsync((CommonResponseDto<OTAGetMemberResponse>)null!);

            var req = new FakeHttpRequestData(_context);
            req.SetMethod("GET");

            var response = await _function.GetMemberById(req, "MEMBER1");

            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns_Error_Status_When_Service_Returns_Failure()
        {
            _service.Setup(s =>
                s.GetMemberByIdAsync("MEMBER1"))
                .ReturnsAsync(new CommonResponseDto<OTAGetMemberResponse>
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

            var response = await _function.GetMemberById(req, "MEMBER1");

            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns_200_When_Service_Returns_Success()
        {
            _service.Setup(s =>
                s.GetMemberByIdAsync("MEMBER1"))
                .ReturnsAsync(SuccessResponse());

            var req = new FakeHttpRequestData(_context);
            req.SetMethod("GET");

            var response = await _function.GetMemberById(req, "MEMBER1");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns_500_When_Exception_Is_Thrown()
        {
            _service.Setup(s =>
                s.GetMemberByIdAsync("MEMBER1"))
                .ThrowsAsync(new Exception("boom"));

            var req = new FakeHttpRequestData(_context);
            req.SetMethod("GET");

            var response = await _function.GetMemberById(req, "MEMBER1");

            Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        private static CommonResponseDto<OTAGetMemberResponse> SuccessResponse() =>
            new()
            {
                Success = true,
                Data = new OTAGetMemberResponse
                {
                    CorrelationID = "corr-1",
                    TradingAgreementCode = "HGV",
                    MemberStatus = "ACTIVE",
                    MemberProfile = new MemberProfile
                    {
                        FirstName = "John",
                        LastName = "Doe"
                    },
                    LoyaltyPrograms = []
                }
            };
    }
}
