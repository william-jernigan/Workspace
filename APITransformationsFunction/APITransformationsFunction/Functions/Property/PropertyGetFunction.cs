using APITransformationsFunction.Common;
using APITransformationsFunction.Model.Property;
using APITransformationsFunction.Model.Shared;
using APITransformationsFunction.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace APITransformationsFunction.Functions.Property
{
    public class PropertyGetFunction(IPropertyService propertyService, ILogger<PropertyGetFunction> logger)
    {
        private readonly ILogger<PropertyGetFunction> _logger = logger;
        private readonly IPropertyService _propertyService = propertyService;
        private static readonly JsonSerializerOptions JsonIndentedOptions = new() { WriteIndented = true };

        [OpenApiOperation(operationId: "GetPropertyByCode", Summary = "Get Property By Code")]
        [OpenApiParameter(name: "propertyCode", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "Property Code", Description = "The Property Code of the property to fetch")]
        [OpenApiParameter(name: "includeMedia", In = ParameterLocation.Query, Required = false, Type = typeof(string), Summary = "Include Media", Description = "Specifies whether to include media details")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(CommonResponseDto<OTAPropertySearchResponse>), Summary = "The response", Description = "Returns the property details")]

        [Function("GetPropertyByCode")]
        public async Task<HttpResponseData> GetPropertyByCode(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "/properties/{propertyCode}")] HttpRequestData req, string propertyCode)
        {
            _logger.LogInformation("[Function][PropertyGetFunction] [START] GET /properties/{PropertyCode} received", propertyCode);

            if (string.IsNullOrWhiteSpace(propertyCode))
            {
                _logger.LogWarning("[Function][PropertyGetFunction] [VALIDATION] PropertyCode is empty");
                return ErrorResponseHelper.CreateBadRequestResponse(req, "PropertyCode cannot be empty.");
            }

            try
            {
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var includeMedia = string.IsNullOrEmpty(query["includeMedia"]) ? "NONE" : query["includeMedia"];
                _logger.LogInformation("[Function][PropertyGetFunction] [CALL] Calling PropertyService.GetPropertyByCodeAsync for {PropertyCode}, includeMedia={IncludeMedia}", propertyCode, includeMedia);
                var property = await _propertyService.GetPropertyByCodeAsync(propertyCode, includeMedia);

                var responsePayload = JsonSerializer.Serialize(property, JsonIndentedOptions);
                _logger.LogInformation("[Function][PropertyGetFunction] [RESPONSE] Payload for {PropertyCode}:\n{Payload}", propertyCode, responsePayload);

                if (property == null)
                {
                    _logger.LogError("[Function][PropertyGetFunction] [NOTFOUND] No results for {PropertyCode}", propertyCode);
                    return ErrorResponseHelper.CreateNotFoundResponse(req, $"Property with code {propertyCode} not found.");
                }

                HttpStatusCode statusCode;
                object responseBody;

                if (property.Success && property.Data != null)
                {
                    statusCode = HttpStatusCode.OK;

                    if (property.Data.Properties != null && property.Data.Properties.Count == 1)
                    {
                        responseBody = new
                        {
                            correlationID = property.Data.CorrelationID,
                            tradingAgreementCode = property.Data.TradingAgreementCode,
                            pagination = property.Data.Pagination,
                            properties = property.Data.Properties.First()
                        };
                    }
                    else
                    {
                        responseBody = property.Data;
                    }
                }
                else
                {
                    statusCode = property.Error?.Status > 0 ? (HttpStatusCode)property.Error.Status : HttpStatusCode.InternalServerError;
                    responseBody = property.Error!;
                }

                _logger.LogInformation("[Function][PropertyGetFunction] [END] Completed with status {StatusCode}", statusCode);
                return await req.CreateJsonResponse(responseBody, statusCode);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Function][PropertyGetFunction] [EXCEPTION] GET /properties/{PropertyCode} failed", propertyCode);
                return ErrorResponseHelper.CreateInternalServerErrorResponse(req);
            }
        }
    }
}
