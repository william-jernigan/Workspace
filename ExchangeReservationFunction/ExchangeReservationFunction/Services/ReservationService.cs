using ExchangeReservationFunction.Common;
using ExchangeReservationFunction.Mappers;
using ExchangeReservationFunction.Model.Reservation.Create.Request;
using ExchangeReservationFunction.Model.Reservation.Create.Response;
using ExchangeReservationFunction.Model.Reservation.GetReservation;
using ExchangeReservationFunction.Model.Reservation.Search;
using ExchangeReservationFunction.Model.Common;
using ExchangeReservationFunction.Services.Interfaces;
using ExchangeReservationFunction.Validators;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ExchangeReservationFunction.Services
{
    public partial class ReservationService(HttpClient httpClient, ILogger<ReservationService> logger) : IReservationService
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly ILogger<ReservationService> _logger = logger;
        private static readonly JsonSerializerOptions JsonIndentedOptions = new() { WriteIndented = true };

        public async Task<CommonResponseDto<OTAGetReservationResponse>> GetReservationByIdAsync(string reservationId)
        {
            _logger.LogInformation("{Prefix} [START] GetReservationByIdAsync called for ReservationId={ReservationId}", ApiConstants.APITransformationFunctionPrefix, reservationId);
            // Validate reservationId — only letters and numbers allowed
            if (string.IsNullOrWhiteSpace(reservationId) ||
                !MyRegex().IsMatch(reservationId))
            {
                _logger.LogWarning("{Prefix} [EXCEPTION] Invalid reservationId format: {ReservationId}", ApiConstants.APITransformationFunctionPrefix, reservationId);
                var errorResponse = new HttpResponseMessage(HttpStatusCode.BadRequest);
                return new CommonResponseDto<OTAGetReservationResponse>
                {
                    Success = false,
                    Error = CommonMapper.MapBackendError(backendResponse: errorResponse, context: "reservation")
                };
            }
            try
            {
                _logger.LogInformation("{Prefix} [Backend][START] Calling backend GetReservationById for {ReservationId}", ApiConstants.APITransformationFunctionPrefix, reservationId);

                var backendResponse = await _httpClient.GetAsync($"{ApiConstants.ReservationUrl}/{reservationId}");
                var backendContent = await backendResponse.Content.ReadAsStringAsync();
                _logger.LogInformation("{Prefix} [Backend] Incoming backend response payload:\n{Payload}", ApiConstants.APITransformationFunctionPrefix, backendContent);
                //When the HTTP call itself fails (non-success status code)
                if (!backendResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("{Prefix} [Backend][END] Backend Reservation API returned an error for reservation {ReservationId}", ApiConstants.APITransformationFunctionPrefix, reservationId);
                    return new CommonResponseDto<OTAGetReservationResponse>
                    {
                        Success = false,
                        Error = CommonMapper.MapBackendError(backendResponse: backendResponse, context: "reservation")
                    };
                }
                var sourceResponse = JsonSerializer.Deserialize<OnexReservationRequest>(backendContent, JsonOptionsProvider.Options);

                //When the response is successful but contains an error payload
                if (sourceResponse?.Data == null)
                {
                    _logger.LogError("{Prefix} [Backend][EXCEPTION] Backend Reservation API returned null/invalid data for {ReservationId}", ApiConstants.APITransformationFunctionPrefix, reservationId);
                    var errorResponse = new HttpResponseMessage(HttpStatusCode.NotFound);
                    return new CommonResponseDto<OTAGetReservationResponse>
                    {
                        Success = false,
                        Error = CommonMapper.MapBackendError(backendResponse: errorResponse, context: "reservation")
                    };
                }

                // Success - map to OTA DTO
                var otaResponse = ReservationMapper.MapToOTAResponse(sourceResponse);
                var otaResponseJson = JsonSerializer.Serialize(otaResponse, JsonIndentedOptions);
                _logger.LogInformation("{Prefix} OTA Response after Mapping:\n{Payload}", ApiConstants.APITransformationFunctionPrefix, otaResponseJson);

                _logger.LogInformation("{Prefix} [END] GetReservationByIdAsync succeeded for {ReservationId}", ApiConstants.APITransformationFunctionPrefix, reservationId);
                return new CommonResponseDto<OTAGetReservationResponse>
                {
                    Success = true,
                    Data = otaResponse
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} [EXCEPTION] Error fetching reservation data for {ReservationId}", ApiConstants.APITransformationFunctionPrefix, reservationId);

                return new CommonResponseDto<OTAGetReservationResponse>
                {
                    Success = false,
                    Error = new OTAErrorResponse
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

        public async Task<CommonResponseDto<OTACreateReservationResponse>> CreateReservationAsync(OTACreateReservationRequest otaRequest)
        {
            var correlationId = otaRequest.CorrelationID;
            using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))

            {
                try
                {
                    _logger.LogInformation("{Prefix} [START] CreateReservationAsync called.", ApiConstants.APITransformationFunctionPrefix);
                    var requestJson = JsonSerializer.Serialize(otaRequest, JsonIndentedOptions);
                    _logger.LogInformation("{Prefix} Incoming OTACreateReservationRequest payload:\n{Payload}", ApiConstants.APITransformationFunctionPrefix, requestJson);

                    var validator = new OTACreateReservationRequestValidator();
                    var validationResult = validator.Validate(otaRequest);

                    if (!validationResult.IsValid)
                    {
                        var errors = validationResult.Errors.Select(e => $"Property: {e.PropertyName} → Error: {e.ErrorMessage}").ToList();
                        _logger.LogWarning("{Prefix} [EXCEPTION] New reservation request is not valid. Validation failed with - {Errors}", ApiConstants.APITransformationFunctionPrefix, string.Join(Environment.NewLine, errors));
                        var errorResponse = new HttpResponseMessage(HttpStatusCode.BadRequest);
                        return new CommonResponseDto<OTACreateReservationResponse>
                        {
                            Success = false,
                            Error = CommonMapper.MapBackendError(backendResponse: errorResponse, context: "reservation", validationMessages: errors)
                        };
                    }
                    _logger.LogInformation("{Prefix} [Mapping] Mapping OTA request to ONEX CreateReservation request.", ApiConstants.APITransformationFunctionPrefix);
                    // 1. Map OTA to Onex
                    var onexRequest = ReservationMapper.MapToOnexRequest(otaRequest);
                    var onexRequestJson = JsonSerializer.Serialize(onexRequest, JsonIndentedOptions);
                    _logger.LogInformation("{Prefix} ONEX Request payload after Mapping:\n{Payload}", ApiConstants.APITransformationFunctionPrefix, onexRequestJson);

                    // 2. Call backend API
                    _logger.LogInformation("{Prefix} [Backend][START] Calling backend CreateReservation.", ApiConstants.APITransformationFunctionPrefix);

                    var content = new StringContent(JsonSerializer.Serialize(onexRequest), Encoding.UTF8, "application/json");
                    var url = ApiConstants.ReservationUrl;
                    var response = await _httpClient.PostAsync(url, content, new CancellationTokenSource().Token);

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError("{Prefix} [Backend][EXCEPTION] Backend returned error while creating reservation.", ApiConstants.APITransformationFunctionPrefix);
                        return new CommonResponseDto<OTACreateReservationResponse>
                        {
                            Success = false,
                            Error = CommonMapper.MapBackendError(backendResponse: response)
                        };
                    }

                    // 3. Deserialize backend response

                    var backendJson = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("{Prefix} [Backend] Incoming backend CreateReservation payload:\n{Payload}", ApiConstants.APITransformationFunctionPrefix, backendJson);
                    var sourceResponse = JsonSerializer.Deserialize<ReservationResponse>(backendJson, JsonOptionsProvider.Options);

                    if (sourceResponse?.Data == null)
                    {
                        var bookingError = sourceResponse?.Data?.Error;

                        _logger.LogError("{Prefix} [EXCEPTION] Reservation service returned error while creating new reservation. ErrorCode: {ErrorCode}, ErrorMessage: {ErrorMessage}",
                            ApiConstants.APITransformationFunctionPrefix, bookingError?.ErrorCode, bookingError?.ErrorMessage);

                        // Map BookingError to shared Error
                        var sharedError = new ReservationError
                        {
                            ErrorCode = bookingError?.ErrorCode,
                            ErrorMessage = bookingError?.ErrorMessage,
                            ErrorType = bookingError?.ErrorType
                        };

                        return new CommonResponseDto<OTACreateReservationResponse>
                        {
                            Success = false,
                            Error = CommonMapper.MapBackendError(sharedError, context: "reservation")
                        };
                    }


                    // 4. Map Onex to OTA
                    var otaResponse = ReservationMapper.MapOnexToOtaResponse(sourceResponse, otaRequest);
                    var otaResponseJson = JsonSerializer.Serialize(otaResponse, JsonIndentedOptions);
                    _logger.LogInformation("{Prefix} OTA Response after Mapping:\n{Payload}", ApiConstants.APITransformationFunctionPrefix, otaResponseJson);

                    _logger.LogInformation("{Prefix} [END] CreateReservationAsync succeeded.", ApiConstants.APITransformationFunctionPrefix);
                    return new CommonResponseDto<OTACreateReservationResponse>
                    {
                        Success = true,
                        Data = otaResponse
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "{Prefix} [EXCEPTION] Error while creating new reservation", ApiConstants.APITransformationFunctionPrefix);
                    return new CommonResponseDto<OTACreateReservationResponse>
                    {
                        Success = false,
                        Error = new OTAErrorResponse
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
        public async Task<CommonResponseDto<OTASearchReservationResponse>> SearchReservationAsync(OTASearchReservationRequest otaRequest, int page, int pageSize)
        {
            var correlationId = otaRequest.CorrelationID;
            using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))

            {
                try
                {
                    _logger.LogInformation("{Prefix} [START] SearchReservationAsync called.", ApiConstants.APITransformationFunctionPrefix);
                    var requestJson = JsonSerializer.Serialize(otaRequest, JsonIndentedOptions);
                    _logger.LogInformation("{Prefix} Incoming OTASearchReservationRequest payload:\n{Payload}", ApiConstants.APITransformationFunctionPrefix, requestJson);

                    var validator = new OTASearchReservationRequestValidator();
                    var validationResult = validator.Validate(otaRequest);

                    if (!validationResult.IsValid)
                    {
                        var errors = string.Join(", ", validationResult.Errors.Select(e => $"Property: {e.PropertyName}, Error: {e.ErrorMessage}"));

                        _logger.LogWarning("{Prefix} [EXCEPTION] Search reservation request is not valid. Validation failed with - {Errors}",
                            ApiConstants.APITransformationFunctionPrefix, errors);

                        var errorResponse = new HttpResponseMessage(HttpStatusCode.BadRequest);
                        return new CommonResponseDto<OTASearchReservationResponse>
                        {
                            Success = false,
                            Error = CommonMapper.MapBackendError(backendResponse: errorResponse, context: "reservation")
                        };
                    }

                    _logger.LogInformation("{Prefix} [Mapping] Mapping OTA search request to ONEX format.", ApiConstants.APITransformationFunctionPrefix);

                    // Map OTA To Onex
                    var onexRequest = ReservationMapper.MapToSearchOnexRequest(otaRequest);
                    var onexRequestJson = JsonSerializer.Serialize(onexRequest, JsonIndentedOptions);
                    _logger.LogInformation("{Prefix} ONEX Request payload after Mapping:\n{Payload}", ApiConstants.APITransformationFunctionPrefix, onexRequestJson);

                    // Call backend API
                    _logger.LogInformation("{Prefix} [Backend][START] Calling backend SearchReservation API.", ApiConstants.APITransformationFunctionPrefix);
                    var backendContent = new StringContent(JsonSerializer.Serialize(onexRequest, JsonOptionsProvider.Options), Encoding.UTF8, "application/json");
                    var backendResponse = await _httpClient.PostAsync($"{ApiConstants.ReservationUrl}/search?page={page}&pageSize={pageSize}", backendContent);

                    if (!backendResponse.IsSuccessStatusCode)
                    {
                        _logger.LogError("{Prefix} - [Backend][END] Reservation service returned error while getting reservation details.", ApiConstants.APITransformationFunctionPrefix);
                        return new CommonResponseDto<OTASearchReservationResponse>
                        {
                            Success = false,
                            Error = CommonMapper.MapBackendError(backendResponse: backendResponse)
                        };
                    }

                    // Deserialize backend response

                    var backendJson = await backendResponse.Content.ReadAsStringAsync();
                    _logger.LogInformation("{Prefix} [Backend] Incoming backend SearchReservation payload:\n{Payload}", ApiConstants.APITransformationFunctionPrefix, backendJson);
                    var sourceResponse = JsonSerializer.Deserialize<OnexSearchReservationResponse>(backendJson, JsonOptionsProvider.Options);

                    var otaResponse = ReservationMapper.MapSearchOnexToOtaResponse(sourceResponse);
                    var otaResponseJson = JsonSerializer.Serialize(otaResponse, JsonIndentedOptions);
                    _logger.LogInformation("{Prefix} OTA Response after Mapping:\n{Payload}", ApiConstants.APITransformationFunctionPrefix, otaResponseJson);

                    _logger.LogInformation("{Prefix} [END] SearchReservationAsync succeeded.", ApiConstants.APITransformationFunctionPrefix);
                    return new CommonResponseDto<OTASearchReservationResponse>
                    {
                        Success = true,
                        Data = otaResponse
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "{Prefix} [EXCEPTION] Error while fetching reservation details", ApiConstants.APITransformationFunctionPrefix);

                    return new CommonResponseDto<OTASearchReservationResponse>
                    {
                        Success = false,
                        Error = new OTAErrorResponse
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

        [System.Text.RegularExpressions.GeneratedRegex("^[a-zA-Z0-9]+$")]
        private static partial System.Text.RegularExpressions.Regex MyRegex();
    }
}
