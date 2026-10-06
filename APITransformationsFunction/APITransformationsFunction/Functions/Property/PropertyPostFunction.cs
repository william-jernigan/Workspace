#nullable enable
using APITransformationsFunction.Common;
using APITransformationsFunction.Model.Property;
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

namespace APITransformationsFunction.Functions.Property
{
    public class PropertyPostFunction(IPropertyService propertyService, ILogger<PropertyPostFunction> logger)
    {
        private readonly ILogger<PropertyPostFunction> _logger = logger;
        private readonly IPropertyService _propertyService = propertyService;
        private static readonly JsonSerializerOptions JsonIndentedOptions = new() { WriteIndented = true };

        [OpenApiOperation(operationId: "GetProperties", Summary = "Retrieve Property Details", Description = "Retrieves property details based on search filters.", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(CommonResponseDto<OTAPropertySearchResponse>), Summary = "Retrieve Property Details", Description = "Returns the property search response.")]

        [Function("GetProperties")]
        public async Task<HttpResponseData> GetProperties(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "/properties")] HttpRequestData req)
        {
            _logger.LogInformation("[Function][PropertyPostFunction] [START] POST /properties request received.");

            try
            {

                if (!ContentTypeValidator.IsJsonRequest(req, out var contentType))
                {
                    _logger.LogError("[Function][PropertyPostFunction] [EXCEPTION] Unsupported request format. Expected JSON but got: {ContentType}", contentType);
                    return ErrorResponseHelper.CreateNotAcceptableResponse(req, "Only application/json is supported.");
                }
                // Read request body
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
                    _logger.LogError(ex, "[Function][PropertyPostFunction] [EXCEPTION] Error reading request body.");
                    return ErrorResponseHelper.CreateBadRequestResponse(req, "Unable to read request body.");
                }

                _logger.LogInformation("[Function][PropertyPostFunction] [REQUEST] Payload:\n{Payload}", requestBody);

                // Deserialize to OTA request model
                OTAPropertySearchRequest? otaRequest;
                try
                {
                    otaRequest = JsonSerializer.Deserialize<OTAPropertySearchRequest>(requestBody, JsonOptionsProvider.Options);

                    if (otaRequest == null)
                    {
                        _logger.LogWarning("[Function][PropertyPostFunction] [VALIDATION] Empty or null request body.");
                        return ErrorResponseHelper.CreateBadRequestResponse(req, "Request body is empty or null.");
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "[Function][PropertyPostFunction] [EXCEPTION] Invalid JSON in request body.");
                    return ErrorResponseHelper.CreateBadRequestResponse(req, "Invalid JSON in request body.");
                }

                // Extract optional query params (page, pageSize)
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);

                var page = 1;
                if (!string.IsNullOrEmpty(query["page"]) && int.TryParse(query["page"], out var parsedPage))
                {
                    page = parsedPage;
                }

                var pageSize = 20;
                if (!string.IsNullOrEmpty(query["pageSize"]) && int.TryParse(query["pageSize"], out var parsedPageSize))
                {
                    pageSize = parsedPageSize;
                }
                _logger.LogInformation("[Function][PropertyPostFunction] [CALL] Calling PropertyService.GetPropertiesAsync (page={Page}, pageSize={PageSize})", page, pageSize);
                // Call the service
                var otaResponse = await _propertyService.GetPropertiesAsync(otaRequest, page, pageSize);
                var responsePayload = JsonSerializer.Serialize(otaResponse, JsonIndentedOptions);
                _logger.LogInformation("[Function][PropertyPostFunction] [RESPONSE] Payload:\n{Payload}", responsePayload);

                var statusCode = otaResponse.Success ? HttpStatusCode.OK : (HttpStatusCode)otaResponse.Error!.Status;
                var responseBody = otaResponse.Success ? otaResponse.Data : (object?)otaResponse.Error;

                _logger.LogInformation("[Function][PropertyPostFunction] [END] Completed with status {StatusCode}", statusCode);
                return await req.CreateJsonResponse(responseBody, statusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Function][PropertyPostFunction] [EXCEPTION] Error processing POST /properties request.");
                return ErrorResponseHelper.CreateInternalServerErrorResponse(req, ex.Message);
            }
        }
    }
}
