#nullable enable
using APITransformationsFunction.Common;
using APITransformationsFunction.Model.Reservation.Search;
using APITransformationsFunction.Model.Shared;
using APITransformationsFunction.Services.Interfaces;
using APITransformationsFunction.Validators;
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

namespace APITransformationsFunction.Functions.Reservation
{
    public class ReservationSearchFunction(IReservationService reservationService, ILogger<ReservationGetFunction> logger)
    {
        private readonly ILogger<ReservationGetFunction> _logger = logger;
        private readonly IReservationService _reservationService = reservationService;
        private static readonly JsonSerializerOptions JsonIndentedOptions = new() { WriteIndented = true };

        [OpenApiOperation(operationId: "SearchReservations", Summary = "Search Reservations")]
        [OpenApiParameter(name: "page", In = ParameterLocation.Query, Required = false, Type = typeof(int), Summary = "Page number", Description = "The page number for pagination")]
        [OpenApiParameter(name: "pageSize", In = ParameterLocation.Query, Required = false, Type = typeof(int), Summary = "Page size", Description = "Number of records per page")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(CommonResponseDto<OTASearchReservationResponse>), Summary = "The response", Description = "Returns the reservation details")]

        [Function("SearchReservations")]
        public async Task<HttpResponseData> SearchReservation([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "reservations/search")] HttpRequestData req)
        {
            _logger.LogInformation("[Function][SearchReservations] [START] POST /reservations/search received");

            try
            {
                // Parse page/pageSize 
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                int page = 1, pageSize = 20;
                if (!string.IsNullOrEmpty(query["page"]) && !int.TryParse(query["page"], out page))
                {
                    page = 1;
                }

                if (!string.IsNullOrEmpty(query["pageSize"]) && !int.TryParse(query["pageSize"], out pageSize))
                {
                    pageSize = 20;
                }

                page = Math.Max(page, 1);
                pageSize = Math.Clamp(pageSize, 1, 100);

                if (!ContentTypeValidator.IsJsonRequest(req, out var contentType))
                {
                    _logger.LogError("[Function][ReservationSearchFunction] [EXCEPTION] Unsupported request format. Expected JSON but got: {ContentType}", contentType);
                    return ErrorResponseHelper.CreateNotAcceptableResponse(req, "Only application/json is supported.");
                }

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
                    _logger.LogWarning("[Function][SearchReservations] [EXCEPTION] Failed to read request body, Error: {Err}", ex.Message);
                    return ErrorResponseHelper.CreateBadRequestResponse(req, "Unable to read request body.");
                }

                _logger.LogInformation("[Function][SearchReservations] [REQUEST] Payload:\n{Payload}", requestBody);

                OTASearchReservationRequest? otaRequest;
                try
                {
                    otaRequest = JsonSerializer.Deserialize<OTASearchReservationRequest>(requestBody, JsonOptionsProvider.Options);

                    if (otaRequest == null)
                    {
                        _logger.LogWarning("[Function][SearchReservation] [VALIDATION] Null or empty request body.");
                        return ErrorResponseHelper.CreateBadRequestResponse(req, "Request body is empty or null.");
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "[Function][SearchReservation] [EXCEPTION] Invalid JSON in request body.");
                    return ErrorResponseHelper.CreateBadRequestResponse(req, "Invalid JSON in request body.");
                }

                _logger.LogInformation("[Function][SearchReservations] [CALL] Calling ReservationService.SearchReservationAsync(page={Page}, pageSize={PageSize})", page, pageSize);

                var reservation = await _reservationService.SearchReservationAsync(otaRequest, page, pageSize);

                var reservationJson = JsonSerializer.Serialize(reservation, JsonIndentedOptions);
                _logger.LogInformation("[Function][SearchReservations] [RESPONSE] Payload:\n{Payload}", reservationJson);

                if (reservation == null)
                {
                    _logger.LogWarning("[Function][SearchReservations] [VALIDATION] No reservations found matching the specified criteria.");
                    return ErrorResponseHelper.CreateNotFoundResponse(req, "No reservations found matching the specified criteria.");
                }

                // 4. Create HTTP response
                var statusCode = reservation.Success ? HttpStatusCode.OK : (HttpStatusCode)reservation.Error!.Status;
                var responseBody = reservation.Success ? reservation.Data : (object?)reservation.Error;

                _logger.LogInformation("[Function][SearchReservations] [END] POST /reservations/search completed.");
                return await req.CreateJsonResponse(responseBody, statusCode);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Function][SearchReservation][EXCEPTION] Error processing Search reservation request - {Error} {Prefix}", ex.Message, ApiConstants.APITransformationFunctionPrefix);
                return ErrorResponseHelper.CreateInternalServerErrorResponse(req);
            }
        }

    }
}



