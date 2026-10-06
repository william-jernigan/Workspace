using APITransformationsFunction.Common;
using APITransformationsFunction.Model.Member;
using APITransformationsFunction.Model.Shared;
using APITransformationsFunction.Services.Interfaces;
using APITransformationsFunction.Validators;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace APITransformationsFunction.Functions.Member
{
    public class MemberUpdateFunction(IMemberService memberService, ILogger<MemberUpdateFunction> logger)
    {
        private readonly ILogger<MemberUpdateFunction> _logger = logger;
        private readonly IMemberService _memberService = memberService;
        private static readonly JsonSerializerOptions JsonIndentedOptions = new() { WriteIndented = true };
        [OpenApiOperation(operationId: "UpdateMember", Summary = "Update Member By Id")]
        [OpenApiParameter(name: "memberId", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "Member Id", Description = "The Member Id of the member to update")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(CommonResponseDto<OTAGetMemberResponse>), Summary = "The response", Description = "Returns the updated member details")]

        [Function("UpdateMember")]
        public async Task<HttpResponseData> UpdateMemberById([HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "members/{memberId}")] HttpRequestData req, string memberId)
        {
            _logger.LogInformation("[Function][MemberUpdateFunction] [START] PATCH /members/{MemberId} request received", memberId);

            if (string.IsNullOrWhiteSpace(memberId))
            {
                _logger.LogWarning("[Function][MemberUpdateFunction] [VALIDATION] MemberId is empty");
                return ErrorResponseHelper.CreateBadRequestResponse(req, "MemberId cannot be empty.");
            }
            try
            {
                if (!ContentTypeValidator.IsJsonRequest(req, out var contentType))
                {
                    _logger.LogError("[Function][MemberUpdateFunction] [EXCEPTION] Unsupported request format. Expected JSON but got: {ContentType}", contentType);
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
                    _logger.LogError(ex, "[Function][MemberUpdateFunction] [EXCEPTION] Error reading request body.");
                    return ErrorResponseHelper.CreateBadRequestResponse(req, "Unable to read request body.");
                }
                if (string.IsNullOrWhiteSpace(requestBody))
                {
                    _logger.LogWarning("[Function][MemberUpdateFunction] [VALIDATION] Request body cannot be empty in the Patch Operation.");
                    return ErrorResponseHelper.CreateBadRequestResponse(req, "Request body cannot be empty in the Patch Operation.");
                }
                _logger.LogInformation("[Function][MemberUpdateFunction] [REQUEST] Patch payload:\n{Payload}", requestBody);
                var patchDoc = DeserializePatchDocument(requestBody);
                if (patchDoc == null)
                {
                    _logger.LogWarning("[Function][MemberUpdateFunction] [VALIDATION] Patch document is invalid or malformed.");
                    return ErrorResponseHelper.CreateBadRequestResponse(req, "Patch document is invalid or malformed in the Patch Operation.");
                }

                // 2. Call service
                _logger.LogInformation("[Function][MemberUpdateFunction] [CALL] Calling MemberService.UpdateMemberAsync for {MemberId}", memberId);
                var result = await _memberService.UpdateMemberAsync(memberId, patchDoc);
                var responsePayload = JsonSerializer.Serialize(result, JsonIndentedOptions);
                _logger.LogInformation("[Function][MemberUpdateFunction] [RESPONSE] Payload for {MemberId}:\n{Payload}", memberId, responsePayload);


                // 3. Create HTTP response               
                if (!result.Success)
                {
                    _logger.LogError("[Function][MemberUpdateFunction] [ERROR] Error updating member.");
                    return await req.CreateJsonResponse(result.Error, (HttpStatusCode)(result.Error?.Status ?? 400));
                }

                _logger.LogInformation("[Function][MemberUpdateFunction] [END] Member details updated for {MemberId}", memberId);
                return await req.CreateJsonResponse(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Function][MemberUpdateFunction] [EXCEPTION] Error processing PATCH /members/{MemberId} request", memberId);
                return ErrorResponseHelper.CreateInternalServerErrorResponse(req);
            }
        }
        private JsonPatchDocument<OTAGetMemberResponse> DeserializePatchDocument(string requestBody)
        {
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                return null;
            }

            try
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<JsonPatchDocument<OTAGetMemberResponse>>(requestBody,
                    new Newtonsoft.Json.JsonSerializerSettings
                    {
                        ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " [Function][DeserializePatchDocument] [EXCEPTION] Failed to deserialize JSON Patch document.");
                return null;
            }
        }

    }

}

