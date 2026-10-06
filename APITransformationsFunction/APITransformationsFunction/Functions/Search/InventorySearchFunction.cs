#nullable enable
using APITransformationsFunction.Common;
using APITransformationsFunction.Model.Search;
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

namespace APITransformationsFunction.Functions.Search
{
    public class InventorySearchFunction(IInventorySearchService inventorySearchService, ILogger<InventorySearchFunction> logger)
    {
        private readonly ILogger<InventorySearchFunction> _logger = logger;
        private readonly IInventorySearchService _inventorySearchService = inventorySearchService;
        private static readonly JsonSerializerOptions JsonIndentedOptions = new() { WriteIndented = true };

        [OpenApiOperation(operationId: "Search", Summary = "Search property availability", Description = "Searches available accommodations (hotels, resorts, rentals) based on filters and criteria", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(CommonResponseDto<OTAInventorySearchResponse>), Summary = "Search property availability", Description = "Returns available properties matching the provided criteria.")]

        [Function("InventorySearch")]
        public async Task<HttpResponseData> InventorySearch(
             [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "inventory")] HttpRequestData req)
        {
            _logger.LogInformation("[Function][InventorySearch] [START] POST /inventory received");

            try
            {
                if (!ContentTypeValidator.IsJsonRequest(req, out var contentType))
                {
                    _logger.LogError("[Function][InventorySearch] [EXCEPTION] Unsupported request format. Expected JSON but got: {ContentType}", contentType);
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
                    _logger.LogWarning("[Function][InventorySearch] [EXCEPTION] Failed to read request body, Error: {Err}", ex.Message);
                    return ErrorResponseHelper.CreateBadRequestResponse(req, "Unable to read request body.");
                }

                _logger.LogInformation("[Function][InventorySearch] [REQUEST] Payload:\n{Payload}", requestBody);

                // 2. Deserialize to OTA request
                OTAInventorySearchRequest? otaRequest;

                try
                {
                    otaRequest = JsonSerializer.Deserialize<OTAInventorySearchRequest>(requestBody, JsonOptionsProvider.Options);

                    if (otaRequest == null)
                    {
                        _logger.LogWarning("[Function][InventorySearch] [VALIDATION] Invalid or empty request body.");
                        return ErrorResponseHelper.CreateBadRequestResponse(req, "Invalid or empty request body.");
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "[Function][InventorySearch] [EXCEPTION] Invalid JSON in request body.");
                    return ErrorResponseHelper.CreateBadRequestResponse(req, "Invalid JSON in request body.");
                }
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var page = int.TryParse(query["page"], out var p) ? p : 1;
                var pageSize = int.TryParse(query["pageSize"], out var q) ? q : 20;

                // 3. Call service
                _logger.LogInformation("[Function][InventorySearch] [CALL] Calling InventorySearchService.InventorySearchAsync(page={Page}, pageSize={PageSize})", page, pageSize);

                var otaResponse = await _inventorySearchService.InventorySearchAsync(otaRequest, page, pageSize);

                var responsePayload = JsonSerializer.Serialize(otaResponse, JsonIndentedOptions);
                _logger.LogInformation("[Function][InventorySearch] [RESPONSE] Payload:\n{Payload}", responsePayload);

                // 4. Create HTTP response
                var statusCode = otaResponse.Success ? HttpStatusCode.OK : (HttpStatusCode)(otaResponse.Error?.Status ?? 500);
                var responseBody = otaResponse.Success ? otaResponse.Data! : (object)otaResponse.Error!;

                _logger.LogInformation("[Function][InventorySearch] [END] Completed with status {StatusCode}", statusCode);
                return await req.CreateJsonResponse(responseBody, statusCode);

            }
            catch (Exception ex)
            {
                _logger.LogError("[Function][InventorySearch] [EXCEPTION] Error processing POST request - {error}", ex.Message);
                return ErrorResponseHelper.CreateInternalServerErrorResponse(req, ex.Message);
            }
        }

    }
}
