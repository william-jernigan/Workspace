using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.Linq;

namespace ExchangeReservationFunction.Validators
{
    public static class ContentTypeValidator
    {
        public static bool IsJsonRequest(HttpRequestData req, out string contentType)
        {
            contentType = null;

            // Extract Content-Type
            foreach (var header in req.Headers)
            {
                if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                {
                    contentType = header.Value.FirstOrDefault()?.ToLowerInvariant();
                    break;
                }
            }

            // Default to JSON if missing or text/plain
            if (string.IsNullOrEmpty(contentType) || contentType.Contains("text/plain"))
            {
                contentType = "application/json";
                return true;
            }

            // Accept only JSON content types
            return contentType.Contains("json");
        }
    }

}
