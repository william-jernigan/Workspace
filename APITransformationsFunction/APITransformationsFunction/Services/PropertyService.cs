using APITransformationsFunction.Common;
using APITransformationsFunction.Mappers;
using APITransformationsFunction.Model.Property;
using APITransformationsFunction.Model.Shared;
using APITransformationsFunction.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace APITransformationsFunction.Services
{
    public class PropertyService(HttpClient httpClient, ILogger<PropertyService> logger) : IPropertyService
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly ILogger<PropertyService> _logger = logger;
        private static readonly JsonSerializerOptions JsonIndentedOptions = new() { WriteIndented = true };
        public async Task<CommonResponseDto<OTAPropertySearchResponse>> GetPropertiesAsync(OTAPropertySearchRequest otaRequest, int page, int pageSize)
        {
            var correlationId = otaRequest.CorrelationID;
            using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))

            {
                try
                {
                    _logger.LogInformation("[Service][PropertyService] [START] GetPropertiesAsync called.");

                    var requestJson = JsonSerializer.Serialize(otaRequest, JsonIndentedOptions);
                    _logger.LogInformation("[Service][PropertyService] [REQUEST] OTAPropertySearchRequest payload:\n{Payload}", requestJson);

                    var context = new ValidationContext(otaRequest);
                    var results = new List<ValidationResult>();

                    if (!Validator.TryValidateObject(otaRequest, context, results, true))
                    {
                        var errors = results.Select(r => r.ErrorMessage).ToList();
                        _logger.LogWarning("[Service][PropertyService] [VALIDATION] Validation failed - {Errors}", string.Join("\n - ", errors));

                        var errorResponse = new HttpResponseMessage(HttpStatusCode.BadRequest);
                        return new CommonResponseDto<OTAPropertySearchResponse>
                        {
                            Success = false,
                            Error = CommonMapper.MapBackendError(backendResponse: errorResponse, context: "property", validationMessages: errors)
                        };
                    }

                    // Call backend API
                    _logger.LogInformation("[Service][PropertyService] [Backend][START] Calling backend property search API.");
                    var content = new StringContent(JsonSerializer.Serialize(otaRequest, JsonOptionsProvider.Options), Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync($"{ApiConstants.Version}/{ApiConstants.PropertyEndpoint}/search?pageSize={pageSize}&pageNumber={page}", content);

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError("[Service][PropertyService] [Backend][END] Property service returned error while getting property details.");
                        return new CommonResponseDto<OTAPropertySearchResponse>
                        {
                            Success = false,
                            Error = CommonMapper.MapBackendError(backendResponse: response, context: "property")
                        };
                    }

                    // Deserialize backend response

                    var backendJson = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("[Service][PropertyService] [Backend][RESPONSE] Payload:\n{Payload}", backendJson);

                    var itemsList = JsonSerializer.Deserialize<PagedResult<Item>>(backendJson, JsonOptionsProvider.Options);

                    if (itemsList == null || !itemsList.Items.Any())
                    {
                        _logger.LogError("[Service][PropertyService] [Backend][EXCEPTION] Property service returned no property data.");
                        var errorResponse = new HttpResponseMessage(HttpStatusCode.BadRequest);
                        return new CommonResponseDto<OTAPropertySearchResponse>
                        {
                            Success = false,
                            Error = CommonMapper.MapBackendError(backendResponse: errorResponse, context: "property")
                        };
                    }
                    var sourceResponse = new ONEXPropertySearchResponse
                    {
                        CorrelationId = otaRequest.CorrelationID,
                        Data = new Model.Property.Data
                        {
                            ItemsList = [.. itemsList.Items],
                        }
                    };
                    var otaResponse = PropertyMapper.MapOnexToOtaResponse(sourceResponse);
                    otaResponse.TradingAgreementCode = otaRequest.TradingAgreementCode;

                    var otaResponseJson = JsonSerializer.Serialize(otaResponse, JsonIndentedOptions);
                    _logger.LogInformation("[Service][PropertyService] [RESPONSE] OTA Response:\n{Payload}", otaResponseJson);

                    _logger.LogInformation("[Service][PropertyService] [END] GetPropertiesAsync succeeded.");
                    return new CommonResponseDto<OTAPropertySearchResponse>
                    {
                        Success = true,
                        Data = new OTAPropertySearchResponse
                        {
                            CorrelationID = otaRequest.CorrelationID,
                            TradingAgreementCode = otaRequest.TradingAgreementCode,
                            Pagination = new Pagination
                            {
                                PageSize = itemsList.PageSize,
                                Page = itemsList.CurrentPage,
                                TotalItems = itemsList.TotalItems,
                                TotalPages = itemsList.TotalPages
                            },
                            Properties = otaResponse.Properties
                        }
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Service][PropertyService] [EXCEPTION] Error retrieving all property details.");
                    return new CommonResponseDto<OTAPropertySearchResponse>
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
        public async Task<CommonResponseDto<OTAPropertySearchResponse>> GetPropertyByCodeAsync(string propertyCode, string includeMedia)
        {
            try
            {
                _logger.LogInformation("[Service][PropertyService] [START] GetPropertyByCodeAsync for {PropertyCode}, includeMedia={IncludeMedia}", propertyCode, includeMedia);

                if (string.IsNullOrWhiteSpace(propertyCode))
                {
                    _logger.LogWarning("[Service][PropertyService] [VALIDATION] PropertyCode is empty");
                    var badRequest = new HttpResponseMessage(HttpStatusCode.BadRequest);
                    return new CommonResponseDto<OTAPropertySearchResponse>
                    {
                        Success = false,
                        Error = CommonMapper.MapBackendError(backendResponse: badRequest, context: "property", validationMessages: ["propertyCode is required."])
                    };
                }

                if (!string.IsNullOrWhiteSpace(includeMedia) &&
                    !Enum.TryParse(typeof(ApiConstants.IncludeMedia), includeMedia, true, out _))
                {
                    _logger.LogWarning("[Service][PropertyService] [VALIDATION] Invalid includeMedia value: {IncludeMedia}", includeMedia);

                    var badRequest = new HttpResponseMessage(HttpStatusCode.BadRequest);
                    return new CommonResponseDto<OTAPropertySearchResponse>
                    {
                        Success = false,
                        Error = CommonMapper.MapBackendError(backendResponse: badRequest, context: "property", validationMessages: [$"Invalid includeMedia value: {includeMedia}"])
                    };
                }
                _logger.LogInformation("[Service][PropertyService] [Backend][START] Calling backend property API for {PropertyCode}", propertyCode);
                var backendResponse = await _httpClient.GetAsync($"{ApiConstants.Version}/{ApiConstants.PropertyEndpoint}/{propertyCode}?include-media={includeMedia}");
                var backendContent = await backendResponse.Content.ReadAsStringAsync();
                _logger.LogInformation("[Service][PropertyService] [Backend][RESPONSE] Payload for {PropertyCode}:\n{Payload}", propertyCode, backendContent);
                if (!backendResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("[Service][PropertyService] [Backend][END] Property service failed for {PropertyCode}", propertyCode);

                    return new CommonResponseDto<OTAPropertySearchResponse>
                    {
                        Success = false,
                        Error = CommonMapper.MapBackendError(backendResponse: backendResponse)
                    };
                }

                var itemsList = JsonSerializer.Deserialize<List<Item>>(backendContent, JsonOptionsProvider.Options);

                if (itemsList == null || itemsList.Count == 0)
                {
                    _logger.LogError("[Service][PropertyService] [Backend][EXCEPTION] Property service returned no data for {PropertyCode}", propertyCode);
                    var errorResponse = new HttpResponseMessage(HttpStatusCode.BadRequest);
                    return new CommonResponseDto<OTAPropertySearchResponse>
                    {
                        Success = false,
                        Error = CommonMapper.MapBackendError(backendResponse: errorResponse, context: "property")
                    };
                }

                var sourceResponse = new ONEXPropertySearchResponse
                {
                    CorrelationId = Guid.NewGuid().ToString(),
                    Data = new Model.Property.Data
                    {
                        ItemsList = itemsList
                    }
                };

                var otaResponse = PropertyMapper.MapOnexToOtaResponse(sourceResponse);

                if (string.IsNullOrEmpty(otaResponse.TradingAgreementCode))
                {
                    otaResponse.TradingAgreementCode = "HGV-ARV-HGV";
                }

                var otaResponseJson = JsonSerializer.Serialize(otaResponse, JsonIndentedOptions);
                _logger.LogInformation("[Service][PropertyService] [RESPONSE] OTA Response for {PropertyCode}:\n{Payload}", propertyCode, otaResponseJson);

                _logger.LogInformation("[Service][PropertyService] [END] GetPropertyByCodeAsync succeeded for {PropertyCode}", propertyCode);

                return new CommonResponseDto<OTAPropertySearchResponse>
                {
                    Success = true,
                    Data = otaResponse
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Service][PropertyService] [EXCEPTION] Error fetching property data for {PropertyCode}", propertyCode);

                return new CommonResponseDto<OTAPropertySearchResponse>
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
