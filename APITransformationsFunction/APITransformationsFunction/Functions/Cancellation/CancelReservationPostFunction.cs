#nullable enable

using APITransformationsFunction.Common;
using APITransformationsFunction.Model.Cancellation;
using APITransformationsFunction.Model.Shared;
using APITransformationsFunction.Services.Interfaces;
using APITransformationsFunction.Validators;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace APITransformationsFunction.Functions.Cancellation
{
    public class CancelReservationPostFunction(ICancellationService cancellationService, ILogger<CancelReservationPostFunction> logger)
    {
        private readonly ILogger<CancelReservationPostFunction> _logger = logger;
        private readonly ICancellationService _cancellationService = cancellationService;
        private static readonly JsonSerializerOptions JsonIndentedOptions = new() { WriteIndented = true };

        [OpenApiOperation(operationId: "CancelReservation", Summary = "Cancel an existing reservation", Description = "Cancels a reservation using the reservation ID", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(OTACancelPostResponse), Summary = "Cancel an existing reservation", Description = "Cancels a reservation using the reservation ID, guest first and last name, and loyalty program details for verification")]
        [OpenApiParameter(name: "reservationID", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "Reservation ID", Description = "ID of an existing reservation.")]
        [Function("CancelReservation")]
        public async Task<HttpResponseData> CancelReservation(
             [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "reservations/{reservationID}/cancel")] HttpRequestData req, string reservationID)
        {
            _logger.LogInformation("[Function][CancelReservationPostFunction] [START] POST /reservations/{reservationId} received", reservationID);
            try
            {
                if (!ContentTypeValidator.IsJsonRequest(req, out var contentType))
                {
                    _logger.LogError("[Function][CancelReservationPostFunction] [EXCEPTION] Unsupported request format. Expected JSON but got: {ContentType}", contentType);
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
                    logger.LogWarning("[Function][CancelReservationPostFunction] [EXCEPTION] Error reading request body, Error: {Err}", ex.Message);
                    return ErrorResponseHelper.CreateBadRequestResponse(req, "Unable to read request body.");
                }
                _logger.LogInformation("[Function][CancelReservationPostFunction] [REQUEST] Payload:\n{Payload}", requestBody);

                // 2. Deserialize to OTA request
                OTACancelPostRequest? otaRequest;

                try
                {
                    otaRequest = JsonSerializer.Deserialize<OTACancelPostRequest>(requestBody, JsonOptionsProvider.Options);

                    if (otaRequest == null)
                    {
                        _logger.LogWarning("[Function][CancelReservationPostFunction] [VALIDATION] Request body is empty or null.");
                        return ErrorResponseHelper.CreateBadRequestResponse(req, "Request body is empty or null.");
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "[Function][CancelReservationPostFunction] [EXCEPTION] Invalid JSON in request body.");
                    return ErrorResponseHelper.CreateBadRequestResponse(req, "Invalid JSON in request body.");
                }

                // 3. Call service
                _logger.LogInformation("[Function][CancelReservationPostFunction] [CALL] Calling CancellationService.CancelReservationAsync");

                var otaResponseWrapper = await _cancellationService.CancelReservationAsync(otaRequest, reservationID);

                _logger.LogInformation("[Function][CancelReservationPostFunction][RESPONSE] Response Wrapper:\n{Payload}", JsonSerializer.Serialize(otaResponseWrapper, JsonIndentedOptions));

                // 4. Create HTTP response
                var statusCode = otaResponseWrapper.Success ? HttpStatusCode.OK : (HttpStatusCode)(otaResponseWrapper.Error?.Status ?? 500);
                var responseBody = otaResponseWrapper.Success ? otaResponseWrapper.Data : (object?)otaResponseWrapper.Error;

                _logger.LogInformation("[Function][CancelReservationPostFunction] [END] Completed with status {StatusCode}", statusCode);
                return await req.CreateJsonResponse(responseBody, statusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError("[Function][CancelReservationPostFunction] [EXCEPTION] Error processing POST request - {error}", ex.Message);
                return ErrorResponseHelper.CreateInternalServerErrorResponse(req);
            }
        }

    }
}
