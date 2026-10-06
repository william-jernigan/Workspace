using Microsoft.AspNetCore.Mvc;
using OneExchangeOrderService.Domain.Exceptions;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;

namespace OneExchangeOrderService.WebApi.Middlewares
{
    public class ErrorHandlerMiddleware(RequestDelegate next, ILogger<ErrorHandlerMiddleware> logger)
    {
        private readonly RequestDelegate _next = next;
        private readonly ILogger<ErrorHandlerMiddleware> _logger = logger;

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                var problemDetails = new ProblemDetails()
                {
                    Instance = $"{context.Request.Path}"
                };

                var response = context.Response;

                switch (ex)
                {
                    case LogicException:
                        // custom logic error
                        problemDetails.Title = "Business/Logic Exception.";
                        problemDetails.Status = (int)HttpStatusCode.BadRequest;
                        problemDetails.Detail = ex.Message;
                        break;
                    case InvalidOperationException:
                        problemDetails.Detail = ex.Message;
                        problemDetails.Status = (int)HttpStatusCode.UnprocessableEntity;
                        problemDetails.Title = "Business/Logic Exception.";
                        break;
                    case ColumnDataLimitException:
                        problemDetails.Detail = ex.Message;
                        problemDetails.Status = (int)HttpStatusCode.BadRequest;
                        problemDetails.Title = "Business/Logic Exception.";
                        break;
                    case DataNotFoundException:
                        // Handle Data Not Found error
                        problemDetails.Title = "Data Not Found";
                        problemDetails.Status = (int)HttpStatusCode.BadRequest;
                        problemDetails.Detail = ex.Message;
                        break;
                    case BoomiTimeoutException:
                        // Handle boomi timeouts
                        problemDetails.Title = "Bad Gateway";
                        problemDetails.Status = (int)HttpStatusCode.BadGateway;
                        problemDetails.Detail = ex.Message;
                        break;
                    case ValidationException:
                        problemDetails.Detail = ex.Message;
                        problemDetails.Status = (int)HttpStatusCode.BadRequest;
                        problemDetails.Title = "Request schema Validation failed.";
                        break;
                    default:
                        // unhandled error
                        problemDetails.Status = (int)HttpStatusCode.InternalServerError;
                        break;
                }

                _logger.LogError(ex, "Problem Details: {@problemDetails}", problemDetails);

                response.ContentType = "application/json";
                response.StatusCode = problemDetails.Status.Value;
                var result = JsonSerializer.Serialize(problemDetails);
                await response.WriteAsync(result);
            }
        }
    }
}
