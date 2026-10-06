#nullable enable
using APITransformationsFunction.Common;
using APITransformationsFunction.Model.Member;
using APITransformationsFunction.Model.Shared;
using APITransformationsFunction.Services.Interfaces;
using APITransformationsFunction.Validators;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace APITransformationsFunction.Functions.Member
{
    public class MemberPostFunction(IMemberService memberService, ILogger<MemberPostFunction> logger)
    {
        private readonly ILogger<MemberPostFunction> _logger = logger;
        private readonly IMemberService _memberService = memberService;
        private static readonly JsonSerializerOptions JsonIndentedOptions = new() { WriteIndented = true };

        [OpenApiOperation(operationId: "CreateMember", Summary = "Create new member", Description = "Returns the details of the new member.", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(CommonResponseDto<OTAPostMemberResponse>), Summary = "Create new member", Description = "Returns the details of the new member.")]

        [Function("CreateMember")]
        public async Task<HttpResponseData> CreateMember(
             [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "members")] HttpRequestData req)
        {
            _logger.LogInformation("[Function][MemberPostFunction] [START] POST /members request received.");

            try
            {
                if (!ContentTypeValidator.IsJsonRequest(req, out var contentType))
                {
                    _logger.LogError("[Function][MemberPostFunction] [EXCEPTION] Unsupported request format. Expected JSON but got: {ContentType}", contentType);
                    return ErrorResponseHelper.CreateNotAcceptableResponse(req, "Only application/json is supported.");
                }

                // 1. Read request body
                string requestBody;
                try
                {
                    using (var reader = new StreamReader(req.Body))
                    {
                        requestBody = await reader.ReadToEndAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Function][MemberPostFunction] [EXCEPTION] Error reading request body.");
                    return ErrorResponseHelper.CreateBadRequestResponse(req, "Unable to read request body.");
                }

                _logger.LogInformation("[Function][MemberPostFunction] [REQUEST] Payload:\n{Payload}", requestBody);

                // 2. Deserialize to OTA request
                OTAPostMemberRequest? otaRequest;
                try
                {
                    otaRequest = JsonSerializer.Deserialize<OTAPostMemberRequest>(requestBody, JsonOptionsProvider.Options);

                    if (otaRequest == null)
                    {
                        _logger.LogWarning("[Function][MemberPostFunction] [VALIDATION] Empty or null request body.");
                        return ErrorResponseHelper.CreateBadRequestResponse(req, "Request body is empty or null.");
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "[Function][MemberPostFunction] [EXCEPTION] Invalid JSON in request body.");
                    return ErrorResponseHelper.CreateBadRequestResponse(req, "Invalid JSON in request body.");
                }

                // 3. Call service
                _logger.LogInformation("[Function][MemberPostFunction] [CALL] Calling MemberService.CreateMemberAsync");
                var result = await _memberService.CreateMemberAsync(otaRequest);
                var responsePayload = JsonSerializer.Serialize(result, JsonIndentedOptions);

                _logger.LogInformation("[Function][MemberPostFunction] [RESPONSE] Payload:\n{Payload}", responsePayload);
                if (!result.Success)
                {
                    _logger.LogError("[Function][MemberPostFunction] [ERROR] Error during member creation.");
                    return await req.CreateJsonResponse(result.Error, (HttpStatusCode)(result.Error?.Status ?? 400));
                }


                // 4. Create HTTP response
                _logger.LogInformation("[Function][MemberPostFunction] [END] New member created.");
                return await req.CreateJsonResponse(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Function][MemberPostFunction][EXCEPTION] Error processing POST / members request.");
                return ErrorResponseHelper.CreateInternalServerErrorResponse(req);
            }
        }

    }
}
