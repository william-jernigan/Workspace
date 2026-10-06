using APITransformationsFunction.Common;
using APITransformationsFunction.Mappers;
using APITransformationsFunction.Model.Search;
using APITransformationsFunction.Model.Shared;
using APITransformationsFunction.Services.Interfaces;
using APITransformationsFunction.Validators.Search;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace APITransformationsFunction.Services
{
    public class InventorySearchService(HttpClient httpClient, ILogger<InventorySearchService> logger) : IInventorySearchService
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly ILogger<InventorySearchService> _logger = logger;
        private static readonly JsonSerializerOptions JsonIndentedOptions = new() { WriteIndented = true };
        public async Task<CommonResponseDto<OTAInventorySearchResponse>> InventorySearchAsync(OTAInventorySearchRequest otaRequest, int page, int pageSize)
        {
            var correlationId = otaRequest.CorrelationID;
            using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))

            {
                try
                {
                    _logger.LogInformation("{Prefix} [START] InventorySearchAsync called.", ApiConstants.APITransformationFunctionPrefix);
                    var requestJson = JsonSerializer.Serialize(otaRequest, JsonIndentedOptions);

                    _logger.LogInformation("{Prefix} Incoming OTAInventorySearchRequest payload:\n{Payload}", ApiConstants.APITransformationFunctionPrefix, requestJson);

                    var validator = new OTAInventorySearchRequestValidator();
                    var validationResult = validator.Validate(otaRequest);

                    if (!validationResult.IsValid)
                    {
                        var errors = validationResult.Errors.Select(e => $"Property: {e.PropertyName} → Error: {e.ErrorMessage}").ToList();
                        _logger.LogWarning("{Prefix} [EXCEPTION] Validation failed for InventorySearch request - {Errors}", ApiConstants.APITransformationFunctionPrefix, string.Join(Environment.NewLine, errors));
                        var errorResponse = new HttpResponseMessage(HttpStatusCode.BadRequest);
                        return new CommonResponseDto<OTAInventorySearchResponse>
                        {
                            Success = false,
                            Error = CommonMapper.MapBackendError(backendResponse: errorResponse, context: "search", validationMessages: errors)
                        };
                    }

                    // 1. Map OTA to Onex
                    _logger.LogInformation("{Prefix} [Mapping] Mapping OTA inventory search request to ONEX format.", ApiConstants.APITransformationFunctionPrefix);

                    var onexRequest = InventorySearchMapper.MapOtaToOnexRequest(otaRequest);
                    var onexRequestJson = JsonSerializer.Serialize(onexRequest, JsonIndentedOptions);
                    _logger.LogInformation("{Prefix} ONEX Request payload after Mapping:\n{Payload}", ApiConstants.APITransformationFunctionPrefix, onexRequestJson);

                    // 2. Call backend API
                    _logger.LogInformation("{Prefix} [Backend][START] Calling backend Inventory Search API.", ApiConstants.APITransformationFunctionPrefix);

                    var content = new StringContent(
                        JsonSerializer.Serialize(onexRequest), Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync($"{ApiConstants.SearchEndpoint}?pageNumber={page}&pageSize={pageSize}", content);

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError("{Prefix} [Backend][END] InventorySearch API returned an error.", ApiConstants.APITransformationFunctionPrefix);
                        return new CommonResponseDto<OTAInventorySearchResponse>
                        {
                            Success = false,
                            Error = CommonMapper.MapBackendError(backendResponse: response, context: "search")
                        };
                    }

                    // 3. Deserialize backend response
                    var backendJson = await response.Content.ReadAsStringAsync();

                    _logger.LogInformation("{Prefix} [Backend] Incoming backend response payload:\n{Payload}", ApiConstants.APITransformationFunctionPrefix, backendJson);
                    var sourceResponse = JsonSerializer.Deserialize<ONEXInventorySearchResponse>(
                        backendJson, JsonOptionsProvider.Options);

                    if (sourceResponse == null || sourceResponse?.Data == null)
                    {
                        _logger.LogError("{Prefix} [Backend][EXCEPTION] Backend returned null or invalid inventory data.", ApiConstants.APITransformationFunctionPrefix);

                        return new CommonResponseDto<OTAInventorySearchResponse>
                        {
                            Success = false,
                            Error = new Model.Shared.OTAErrorResponse
                            {
                                Type = "https://api.hgv.com/errors/bad-request",
                                Title = "Bad Request",
                                Status = 400,
                                Detail = "Backend returned null or invalid data",
                                Instance = $"https://api.hgv.com/errors/{Guid.NewGuid()}"
                            }
                        };
                    }

                    // 4. Map Onex to OTA
                    var otaResponse = InventorySearchMapper.MapOnexToOtaResponse(sourceResponse, otaRequest);
                    var otaResponseJson = JsonSerializer.Serialize(otaResponse, JsonIndentedOptions);
                    _logger.LogInformation("{Prefix} OTA Response after Mapping:\n{Payload}", ApiConstants.APITransformationFunctionPrefix, otaResponseJson);

                    _logger.LogInformation("{Prefix} [END] InventorySearchAsync succeeded.", ApiConstants.APITransformationFunctionPrefix);
                    return new CommonResponseDto<OTAInventorySearchResponse>
                    {
                        Success = true,
                        Data = otaResponse
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "{Prefix} [EXCEPTION] Error occurred while performing inventory search.", ApiConstants.APITransformationFunctionPrefix);
                    return new CommonResponseDto<OTAInventorySearchResponse>
                    {
                        Success = false,
                        Error = new Model.Shared.OTAErrorResponse
                        {
                            Type = "https://api.hgv.com/errors/internal-server-error",
                            Title = "Internal Server Error",
                            Status = 500,
                            Detail = "An unexpected error occurred on the server.",
                            Instance = $"https://api.hgv.com/errors/{Guid.NewGuid()}"
                        }
                    };
                }
            }
        }
    }
}
