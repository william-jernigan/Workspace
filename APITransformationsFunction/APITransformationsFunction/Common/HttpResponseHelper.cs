using APITransformationsFunction.Model.Shared;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace APITransformationsFunction.Common
{
    public static class HttpResponseHelper
    {
        public static async Task<HttpResponseData> CreateJsonResponse(this HttpRequestData req, object data, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            var response = req.CreateResponse(statusCode);
            var json = JsonSerializer.Serialize(data, JsonOptionsProvider.Options);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            await response.WriteStringAsync(json);
            return response;
        }
    }
}
