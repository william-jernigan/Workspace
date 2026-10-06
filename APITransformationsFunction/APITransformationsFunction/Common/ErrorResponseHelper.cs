#nullable enable

using APITransformationsFunction.Model.Shared;
using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.Net;
using System.Text.Json;

namespace APITransformationsFunction.Common
{
    public static class ErrorResponseHelper
    {
        public static HttpResponseData CreateBadRequestResponse(HttpRequestData req, string? customDetail = null)
        {
            var errorId = Guid.NewGuid();
            var detailMessage = string.IsNullOrWhiteSpace(customDetail)
                ? "Invalid or missing parameters."
                : $"Invalid or missing parameters: {customDetail}";

            var problemDetails = new
            {
                type = "https://api.hgv.com/errors/bad-request",
                title = "Bad Request",
                status = 400,
                detail = detailMessage,
                instance = $"https://api.hgv.com/errors/{errorId}"
            };

            var response = req.CreateResponse(HttpStatusCode.BadRequest);
            response.Headers.Add("Content-Type", "application/json");
            response.WriteString(JsonSerializer.Serialize(problemDetails, JsonOptionsProvider.Options));
            return response;
        }

        public static HttpResponseData CreateInternalServerErrorResponse(HttpRequestData req, string? detail = null)
        {
            var errorId = Guid.NewGuid();
            var finalDetail = detail == null
                ? "An unexpected error occurred on the server."
                : $"An unexpected error occurred on the server. {detail}";

            var problemDetails = new
            {
                type = "https://api.hgv.com/errors/internal-server-error",
                title = "Internal Server Error",
                status = 500,
                detail = finalDetail,
                instance = $"https://api.hgv.com/errors/{errorId}"
            };

            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            response.Headers.Add("Content-Type", "application/json");
            response.WriteString(JsonSerializer.Serialize(problemDetails, JsonOptionsProvider.Options));
            return response;
        }

        public static HttpResponseData CreateNotFoundResponse(HttpRequestData req, string detailMessage)
        {
            var errorId = Guid.NewGuid();

            var problemDetails = new
            {
                type = "https://api.hgv.com/errors/not-found",
                title = "Not Found",
                status = 404,
                detail = detailMessage ?? "The requested resource was not found.",
                instance = $"https://api.hgv.com/errors/{errorId}"
            };

            var response = req.CreateResponse(HttpStatusCode.NotFound);
            response.Headers.Add("Content-Type", "application/json");
            response.WriteString(JsonSerializer.Serialize(problemDetails, JsonOptionsProvider.Options));
            return response;
        }

        public static HttpResponseData CreateNotAcceptableResponse(HttpRequestData req, string? customDetail = null)
        {
            var errorId = Guid.NewGuid();
            var detailMessage = string.IsNullOrWhiteSpace(customDetail)
               ? "The requested content type is not supported."
               : $"The requested content type is not supported: {customDetail}";

            var problemDetails = new
            {
                type = "https://api.hgv.com/errors/not-acceptable",
                title = "Not Acceptable",
                status = 406,
                detail = detailMessage,
                instance = $"https://api.hgv.com/errors/{errorId}"
            };

            var response = req.CreateResponse(HttpStatusCode.NotAcceptable);
            response.Headers.Add("Content-Type", "application/json");
            response.WriteString(JsonSerializer.Serialize(problemDetails, JsonOptionsProvider.Options));
            return response;
        }


    }

}
