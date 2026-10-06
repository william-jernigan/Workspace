using APITransformationsFunction.Common;
using APITransformationsFunction.Mappers;
using APITransformationsFunction.Model.Cancellation;
using APITransformationsFunction.Model.Shared;
using APITransformationsFunction.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace APITransformationsFunction.Services
{
    public class CancellationService(HttpClient httpClient, ILogger<CancellationService> logger) : ICancellationService
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly ILogger<CancellationService> _logger = logger;
        private static readonly JsonSerializerOptions JsonIndentedOptions = new() { WriteIndented = true };
        public async Task<CommonResponseDto<OTACancelPostResponse>> CancelReservationAsync(OTACancelPostRequest otaRequest, string reservationId)
        {
            var correlationId = otaRequest.CorrelationID;
            using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))

            {
                _logger.LogInformation("{Prefix} [START] CancelReservationAsync called for ReservationId={reservationId}.", ApiConstants.APITransformationFunctionPrefix, reservationId);

                if (string.IsNullOrWhiteSpace(reservationId))
                {
                    _logger.LogWarning("{Prefix} [EXCEPTION] Invalid or empty ReservationID.", ApiConstants.APITransformationFunctionPrefix);

                    var errorResponse = new HttpResponseMessage(HttpStatusCode.BadRequest);
                    return new CommonResponseDto<OTACancelPostResponse>
                    {
                        Success = false,
                        Error = CommonMapper.MapBackendError(backendResponse: errorResponse, context: "reservation")
                    };
                }

                var requestJson = JsonSerializer.Serialize(otaRequest, JsonIndentedOptions);

                _logger.LogInformation("{Prefix} Incoming OTACancelPostRequest payload:\n{Payload}", ApiConstants.APITransformationFunctionPrefix, requestJson);

                var validationErrors = IsCancellationRequestValid(otaRequest);
                if (validationErrors.Count > 0)
                {
                    _logger.LogWarning("{Prefix}[VALIDATION] Cancellation validation failed: {Errors}",
                        ApiConstants.APITransformationFunctionPrefix, string.Join(Environment.NewLine, validationErrors));
                    var errorResponse = new HttpResponseMessage(HttpStatusCode.BadRequest);
                    return new CommonResponseDto<OTACancelPostResponse>
                    {
                        Success = false,
                        Error = CommonMapper.MapBackendError(backendResponse: errorResponse, context: "reservation", validationMessages: validationErrors)
                    };
                }

                try
                {
                    _logger.LogInformation("{Prefix} [Mapping] - CancelReservation: Mapping OTA to ONEX request for reservation {reservationId}", ApiConstants.APITransformationFunctionPrefix, reservationId);

                    // 1. Map OTA to Onex
                    var onexRequest = CancellationMapper.MapOtaToOnexRequest(otaRequest, reservationId);
                    var onexRequestJson = JsonSerializer.Serialize(onexRequest, JsonIndentedOptions);
                    _logger.LogInformation("{Prefix} ONEX Request payload after Mapping:\n{Payload}", ApiConstants.APITransformationFunctionPrefix, onexRequestJson);

                    // 2. Call backend API
                    _logger.LogInformation("{Prefix} [Backend][START] Sending cancellation request to backend reservation API for reservation {reservationId}.", ApiConstants.APITransformationFunctionPrefix, reservationId);

                    var content = new StringContent(JsonSerializer.Serialize(onexRequest, JsonOptionsProvider.Options), Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync($"{ApiConstants.ReservationUrl}/{reservationId}/cancel", content);

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError("{Prefix} [Backend][END] Cancellation backend returned an error for reservation {reservationId}.", ApiConstants.APITransformationFunctionPrefix, reservationId);
                        return new CommonResponseDto<OTACancelPostResponse>
                        {
                            Success = false,
                            Error = CommonMapper.MapBackendError(backendResponse: response, context: "reservation")
                        };
                    }

                    // 3. Deserialize backend response
                    var backendJson = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("{Prefix} [Backend] Incoming backend response payload:\n{Payload}", ApiConstants.APITransformationFunctionPrefix, backendJson);

                    var sourceResponse = JsonSerializer.Deserialize<ONEXCancelPostResponse>(
                        backendJson, JsonOptionsProvider.Options);

                    if (sourceResponse == null)
                    {
                        _logger.LogError("{Prefix} [Backend][EXCEPTION] Backend returned null or invalid cancellation response.", ApiConstants.APITransformationFunctionPrefix);

                        return new CommonResponseDto<OTACancelPostResponse>
                        {
                            Success = false,
                            Error = new Model.Shared.OTAErrorResponse
                            {
                                Type = "https://api.hgv.com/errors/bad-request",
                                Title = "Bad Request",
                                Status = (int)HttpStatusCode.BadRequest,
                                Detail = "Backend returned null or invalid data",
                                Instance = $"https://api.hgv.com/errors/{Guid.NewGuid()}"
                            }
                        };
                    }

                    // 4. Map Onex to OTA
                    var otaResponse = CancellationMapper.MapOnexToOtaResponse(sourceResponse, otaRequest, reservationId);
                    var otaResponseJson = JsonSerializer.Serialize(otaResponse, JsonIndentedOptions);
                    _logger.LogInformation("{Prefix} OTA Response after Mapping:\n{Payload}", ApiConstants.APITransformationFunctionPrefix, otaResponseJson);

                    _logger.LogInformation("{Prefix} [END] CancelReservationAsync succeeded for reservation {reservationId}.", ApiConstants.APITransformationFunctionPrefix, reservationId);
                    return new CommonResponseDto<OTACancelPostResponse>
                    {
                        Success = true,
                        Data = otaResponse
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "{Prefix} [EXCEPTION] Error while cancelling a reservation.", ApiConstants.APITransformationFunctionPrefix);
                    return new CommonResponseDto<OTACancelPostResponse>
                    {
                        Success = false,
                        Error = new Model.Shared.OTAErrorResponse
                        {
                            Type = "https://api.hgv.com/errors/internal-server-error",
                            Title = "Internal Server Error",
                            Status = (int)HttpStatusCode.InternalServerError,
                            Detail = ex.Message,
                            Instance = $"https://api.hgv.com/errors/{Guid.NewGuid()}"
                        }
                    };
                }
            }
        }

        private static List<string> IsCancellationRequestValid(OTACancelPostRequest otaRequest)
        {
            var errors = new List<string>();

            if (otaRequest == null)
            {
                errors.Add("Request body is null.");
                return errors;
            }

            // CorrelationID validation (UUID format)
            var uuidPattern = @"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$";
            if (string.IsNullOrWhiteSpace(otaRequest.CorrelationID) ||
                otaRequest.CorrelationID.Length != 36 ||
                !Regex.IsMatch(otaRequest.CorrelationID, uuidPattern))
            {
                errors.Add("Correlation ID is missing or invalid");
            }

            // TradingAgreementCode required
            if (string.IsNullOrWhiteSpace(otaRequest.TradingAgreementCode) || otaRequest.TradingAgreementCode.Length > 20)
            {
                errors.Add("TradingAgreementCode is missing or invalid");
            }

            // Reservations + SourceReservationIds validation
            if (otaRequest.Reservations?.SourceReservationIds == null ||
                otaRequest.Reservations.SourceReservationIds.Count < 1)
            {
                errors.Add("At least one SourceReservationId is required.");
            }

            if (otaRequest.Reservations?.SourceReservationIds != null)
            {
                var allowedSourceSystems = Enum.GetNames(typeof(ApiConstants.SourceSystemType))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var src in otaRequest.Reservations.SourceReservationIds)
                {
                    if (string.IsNullOrWhiteSpace(src.SourceSystem) ||
                        !allowedSourceSystems.Contains(src.SourceSystem))
                    {
                        errors.Add("SourceReservationIds.SourceSystem is missing or invalid");
                    }

                    if (string.IsNullOrWhiteSpace(src.Id) || src.Id.Length > 50)
                    {
                        errors.Add("SourceReservationIds.Id is missing or invalid");
                    }
                }
            }

            // CancellationInfo validation
            if (otaRequest.CancellationInfo == null)
            {
                errors.Add("CancellationInfo is required.");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(otaRequest.CancellationInfo.CancelReason))
                {
                    errors.Add("CancelReason is required.");
                }

                if (otaRequest.CancellationInfo.CancelReason?.Length > 500)
                {
                    errors.Add("CancelReason exceeds max length 500.");
                }

                if (string.IsNullOrWhiteSpace(otaRequest.CancellationInfo.CancelComments))
                {
                    errors.Add("CancelComments is required.");
                }

                if (otaRequest.CancellationInfo.CancelComments?.Length > 500)
                {
                    errors.Add("CancelComments exceeds max length 500.");
                }

                if (otaRequest.CancellationInfo.CancelDate == default)
                {
                    errors.Add("CancelDate is required.");
                }
            }

            return errors;
        }
    }
}
