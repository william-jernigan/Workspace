#nullable enable
using APITransformationsFunction.Common;
using APITransformationsFunction.Model.Reservation.Get;
using APITransformationsFunction.Model.Shared;
using APITransformationsFunction.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace APITransformationsFunction.Functions.Reservation
{
    public class ReservationGetFunction(IReservationService reservationService, ILogger<ReservationGetFunction> logger)
    {
        private readonly ILogger<ReservationGetFunction> _logger = logger;
        private readonly IReservationService _reservationService = reservationService;
        private static readonly JsonSerializerOptions JsonIndentedOptions = new() { WriteIndented = true };

        [OpenApiOperation(operationId: "GetReservationsById", Summary = "Query reservations with reservation ID.")]
        [OpenApiParameter(name: "reservationID", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "Reservation ID", Description = "The Reservation Id of the reservation to fetch")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(CommonResponseDto<OTAGetReservationResponse>), Summary = "The response", Description = "Returns the reservation details")]

        [Function("GetReservationById")]
        public async Task<HttpResponseData> GetReservationById(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "reservations/{reservationID}")] HttpRequestData req, string reservationID)
        {
            _logger.LogInformation("[Function][GetReservationById] [START] GET /reservations/{ReservationId} received", reservationID);
            if (string.IsNullOrWhiteSpace(reservationID))
            {
                _logger.LogWarning("[Function][GetReservationById] [VALIDATION] Empty or null ReservationID");
                return ErrorResponseHelper.CreateBadRequestResponse(req, "ReservationID cannot be empty.");
            }

            try
            {
                _logger.LogInformation("[Function][GetReservationById] [CALL] Calling ReservationService.GetReservationByIdAsync for {ReservationId}", reservationID);

                var reservation = await _reservationService.GetReservationByIdAsync(reservationID);

                var reservationJson = JsonSerializer.Serialize(reservation, JsonIndentedOptions);
                _logger.LogInformation("[Function][GetReservationById] [RESPONSE] Payload:\n{Payload}", reservationJson);

                if (reservation == null)
                {
                    _logger.LogWarning("[Function][GetReservationById] [EXCEPTION] No results found for the reservationID: {reservationID} - {Prefix}", reservationID, ApiConstants.APITransformationFunctionPrefix);
                    return ErrorResponseHelper.CreateNotFoundResponse(req, "No results found for the reservation Id");
                }

                // 4. Create HTTP response
                var statusCode = reservation.Success ? HttpStatusCode.OK : (HttpStatusCode)reservation.Error!.Status;
                var responseBody = reservation.Success ? reservation.Data : (object?)reservation.Error;

                _logger.LogInformation("[Function][GetReservationById] [END] GET /reservations/{ReservationId} completed.", reservationID);
                return await req.CreateJsonResponse(responseBody, statusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Function][GetReservationById] [EXCEPTION] GET request for reservationID: {ReservationID} failed - {Prefix}", reservationID, ApiConstants.APITransformationFunctionPrefix);
                return ErrorResponseHelper.CreateInternalServerErrorResponse(req);
            }
        }
    }
}
