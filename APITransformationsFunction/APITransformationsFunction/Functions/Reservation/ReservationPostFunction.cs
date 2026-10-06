#nullable enable
using APITransformationsFunction.Common;
using APITransformationsFunction.Model.Reservation.Create.Request;
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

namespace APITransformationsFunction.Functions.Reservation
{
    public class ReservationPostFunction(IReservationService reservationService, ILogger<ReservationPostFunction> logger)
    {
        private readonly ILogger<ReservationPostFunction> _logger = logger;
        private readonly IReservationService _reservationService = reservationService;
        private static readonly JsonSerializerOptions JsonIndentedOptions = new() { WriteIndented = true };

        [OpenApiOperation(operationId: "CreateReservation", Summary = "Create new reservation", Description = "Returns new reservation details.", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(CommonResponseDto<OTACreateReservationResponse>), Summary = "Create new reservation", Description = "Returns new reservation details.")]

        [Function("CreateReservation")]
        public async Task<HttpResponseData> CreateReservation(
             [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "reservations")] HttpRequestData req)
        {
            _logger.LogInformation("[Function][CreateReservation] [START] POST /reservations received");

            try
            {
                if (!ContentTypeValidator.IsJsonRequest(req, out var contentType))
                {
                    _logger.LogError("[Function][CreateReservation] [EXCEPTION] Unsupported request format. Expected JSON but got: {ContentType}", contentType);
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
                    _logger.LogWarning("[Function][CreateReservation] [EXCEPTION] Unable to read request body , Error[Err].", ex.Message);
                    return ErrorResponseHelper.CreateBadRequestResponse(req, "Unable to read request body.");
                }

                _logger.LogInformation("[Function][CreateReservation] [REQUEST] Payload:\n{Payload}", requestBody);

                // 2. Deserialize to OTA request
                OTACreateReservationRequest? otaRequest;

                try
                {
                    otaRequest = JsonSerializer.Deserialize<OTACreateReservationRequest>(requestBody, JsonOptionsProvider.Options);

                    if (otaRequest == null)
                    {
                        _logger.LogWarning("[Function][CreateReservation] [VALIDATION] Null or empty request body.");
                        return ErrorResponseHelper.CreateBadRequestResponse(req, "Request body is empty or null.");
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "[Function][CreateReservation] [EXCEPTION] Invalid JSON in request body.");
                    return ErrorResponseHelper.CreateBadRequestResponse(req, "Invalid JSON in request body.");
                }

                // 3. Call service
                _logger.LogInformation("[Function][CreateReservation] [CALL] Calling ReservationService.CreateReservationAsync");

                var otaResponse = await _reservationService.CreateReservationAsync(otaRequest);

                var responsePayload = JsonSerializer.Serialize(otaResponse, JsonIndentedOptions);
                _logger.LogInformation("[Function][CreateReservation] [RESPONSE] Payload:\n{Payload}", responsePayload);

                // 4. Create HTTP response
                var statusCode = otaResponse.Success ? HttpStatusCode.OK : (HttpStatusCode)otaResponse.Error!.Status;
                var responseBody = otaResponse.Success ? otaResponse.Data : (object?)otaResponse.Error;

                _logger.LogInformation("[Function][CreateReservation] [END] Completed with status {StatusCode}", statusCode);
                return await req.CreateJsonResponse(responseBody, statusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError("[Function][CreateReservation] Error processing POST request - {error}", ApiConstants.APITransformationFunctionPrefix, ex.Message);
                return ErrorResponseHelper.CreateInternalServerErrorResponse(req);
            }
        }

    }
}


