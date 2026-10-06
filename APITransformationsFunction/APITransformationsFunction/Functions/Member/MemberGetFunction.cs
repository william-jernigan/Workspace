using APITransformationsFunction.Common;
using APITransformationsFunction.Model.Member;
using APITransformationsFunction.Model.Shared;
using APITransformationsFunction.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace APITransformationsFunction.Functions.Member
{
    public partial class MemberGetFunction(IMemberService memberService, ILogger<MemberGetFunction> logger)
    {
        private readonly ILogger<MemberGetFunction> _logger = logger;
        private readonly IMemberService _memberService = memberService;
        private static readonly JsonSerializerOptions JsonIndentedOptions = new() { WriteIndented = true };
        [OpenApiOperation(operationId: "GetMemberById", Summary = "Get Member By Id")]
        [OpenApiParameter(name: "memberId", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "Member Id", Description = "The Member Id of the member to fetch")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(CommonResponseDto<OTAGetMemberResponse>), Summary = "The response", Description = "Returns the member details")]

        [Function("GetMemberById")]
        public async Task<HttpResponseData> GetMemberById(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "members/{memberId}")] HttpRequestData req, string memberId)
        {
            _logger.LogInformation("[Function][MemberGetFunction] [START] GET /members/{MemberId} received", memberId);
            if (string.IsNullOrWhiteSpace(memberId))
            {
                _logger.LogWarning("[Function][MemberGetFunction] [VALIDATION] MemberId is empty");
                return ErrorResponseHelper.CreateBadRequestResponse(req, "MemberId cannot be empty.");
            }
            if (!MyRegex().IsMatch(memberId))
            {
                _logger.LogWarning("[Function][MemberGetFunction] [VALIDATION] Invalid memberId format: {MemberId}", memberId);
                return ErrorResponseHelper.CreateBadRequestResponse(req, "MemberId must be uppercase alphanumeric, max 20 characters.");
            }
            try
            {
                _logger.LogInformation("[Function][MemberGetFunction] [CALL] Calling MemberService.GetMemberByIdAsync for {MemberId}", memberId);
                var result = await _memberService.GetMemberByIdAsync(memberId);
                var responsePayload = JsonSerializer.Serialize(result, JsonIndentedOptions);
                _logger.LogInformation("[Function][MemberGetFunction] [RESPONSE] Payload for {MemberId}:\n{Payload}", memberId, responsePayload);

                if (result == null)
                {
                    _logger.LogError("[Function][MemberGetFunction] [NOTFOUND] No results for {MemberId}", memberId);
                    return ErrorResponseHelper.CreateNotFoundResponse(req, $"Member with ID '{memberId}' not found.");
                }
                if (!result.Success)
                {
                    _logger.LogError("[Function][MemberGetFunction] [ERROR] Error getting member details.");
                    return await req.CreateJsonResponse(result.Error, (HttpStatusCode)(result.Error?.Status ?? 400));

                }
                _logger.LogInformation("[Function][MemberGetFunction] [END] Returning response for {MemberId}", memberId);
                return await req.CreateJsonResponse(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Function][MemberGetFunction][EXCEPTION] GET / members /{ MemberId} failed", memberId);
                return ErrorResponseHelper.CreateInternalServerErrorResponse(req);
            }
        }

        [GeneratedRegex(@"^[A-Z0-9]{1,20}$")]
        private static partial Regex MyRegex();
    }
}
