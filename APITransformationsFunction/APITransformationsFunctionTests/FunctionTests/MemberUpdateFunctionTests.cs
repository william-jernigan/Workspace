using APITransformationsFunction.Functions.Member;
using APITransformationsFunction.Model.Cancellation;
using APITransformationsFunction.Model.Member;
using APITransformationsFunction.Model.Shared;
using APITransformationsFunction.Services.Interfaces;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Text;

namespace APITransformationsFunctionTests.FunctionTests
{
    [TestClass]
    public class MemberUpdateFunctionTests
    {
        private Mock<IMemberService> _service;
        private Mock<ILogger<MemberUpdateFunction>> _logger;
        private MemberUpdateFunction _function;
        private FunctionContext _context;

        [TestInitialize]
        public void Setup()
        {
            _service = new Mock<IMemberService>(MockBehavior.Strict);
            _logger = new Mock<ILogger<MemberUpdateFunction>>();
            _context = new Mock<FunctionContext>().Object;

            _function = new MemberUpdateFunction(
                _service.Object,
                _logger.Object
            );
        }

        [TestMethod]
        public async Task Returns_400_When_MemberId_Is_Empty()
        {
            var req = PatchRequest(ValidPatchJson());

            var response = await _function.UpdateMemberById(req, "");

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns_406_When_ContentType_Is_Not_Json()
        {
            var req = new FakeHttpRequestData(_context);
            req.Headers.Add("Content-Type", "text/plain");
            req.SetMethod("PATCH");

            var response = await _function.UpdateMemberById(req, "MEMBER1");

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns_400_When_Body_Is_Empty()
        {
            var req = new FakeHttpRequestData(_context);
            req.Headers.Add("Content-Type", "application/json");
            req.SetMethod("PATCH");

            var response = await _function.UpdateMemberById(req, "MEMBER1");

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns_400_When_Patch_Document_Is_Invalid()
        {
            var req = PatchRequest("{ invalid patch json }");

            var response = await _function.UpdateMemberById(req, "MEMBER1");

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns_Error_Status_When_Service_Returns_Failure()
        {
            _service.Setup(s =>
            s.UpdateMemberAsync(
            "MEMBER1",
            It.IsAny<JsonPatchDocument<OTAGetMemberResponse>>()))
            .ReturnsAsync(new CommonResponseDto<OTAPostMemberResponse>
            {
                Success = false,
                Data = new OTAPostMemberResponse
                {
                    MemberStatus = "ACTIVE"
                }
            });

            var req = PatchRequest(ValidPatchJson());

            var response = await _function.UpdateMemberById(req, "MEMBER1");

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns_200_When_Service_Returns_Success()
        {
            _service.Setup(s =>
            s.UpdateMemberAsync(
            "MEMBER1",
            It.IsAny<JsonPatchDocument<OTAGetMemberResponse>>()))
            .ReturnsAsync(new CommonResponseDto<OTAPostMemberResponse>
            {
                Success = true,
                Data = new OTAPostMemberResponse
                {
                    MemberStatus = "ACTIVE"
                }
            });

            var req = PatchRequest(ValidPatchJson());

            var response = await _function.UpdateMemberById(req, "MEMBER1");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public async Task Returns_500_When_Exception_Is_Thrown()
        {
            _service.Setup(s =>
                s.UpdateMemberAsync("MEMBER1", It.IsAny<JsonPatchDocument<OTAGetMemberResponse>>()))
                .ThrowsAsync(new Exception("boom"));

            var req = PatchRequest(ValidPatchJson());

            var response = await _function.UpdateMemberById(req, "MEMBER1");

            Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        private FakeHttpRequestData PatchRequest(string json)
        {
            var req = new FakeHttpRequestData(_context);
            req.Headers.Add("Content-Type", "application/json");
            req.SetMethod("PATCH");

            var bytes = Encoding.UTF8.GetBytes(json);
            req.Body.Write(bytes, 0, bytes.Length);
            req.Body.Position = 0;

            return req;
        }

        private static string ValidPatchJson() =>
                                 /*lang=json,strict*/
                                 @"[
                {
                    ""op"": ""replace"",
                    ""path"": ""/memberStatus"",
                    ""value"": ""ACTIVE""
                }
            ]";
    }
}
