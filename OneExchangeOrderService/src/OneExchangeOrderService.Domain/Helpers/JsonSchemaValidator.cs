using NJsonSchema;
using System.Text.Json;

namespace OneExchangeOrderService.Domain.Helpers
{
    public static class JsonSchemaValidator
    {
        private static JsonSchema? _reservationRequestSchema;

        // Keep camelCase to match schema keys like id, source, data, etc.
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public static void InitializeReservationRequestSchema(string schemaJson)
        {
            if (string.IsNullOrWhiteSpace(schemaJson))
            {
                throw new ArgumentException("Schema JSON is empty.", nameof(schemaJson));
            }

            // Parse synchronously (safe for startup usage)
            _reservationRequestSchema = JsonSchema.FromJsonAsync(schemaJson).GetAwaiter().GetResult();
        }

        public static (bool isValid, List<(string path, string message)> errors)
            ValidateReservationRequest<T>(T request)
        {
            if (_reservationRequestSchema is null)
            {
                throw new InvalidOperationException("Reservation request schema not initialized.");
            }

            var json = JsonSerializer.Serialize(request, JsonOptions);

            // NJsonSchema returns validation errors with paths like "#/data/..."
            var validationErrors = _reservationRequestSchema.Validate(json);

            if (validationErrors is null || validationErrors.Count == 0)
            {
                return (true, new List<(string path, string message)>());
            }

            var errors = validationErrors
                .Select(e =>
                {
                    var path = string.IsNullOrWhiteSpace(e.Path) ? "$" : NormalizePath(e.Path);
                    var msg = string.IsNullOrWhiteSpace(e.ToString()) ? "Schema validation error" : e.ToString();
                    return (path, msg);
                })
                .ToList();

            return (false, errors);
        }

        private static string NormalizePath(string njsonPath)
        {
            // Example: "#/data/systemMeta/tenantId" -> "$.data.systemMeta.tenantId"
            if (njsonPath.StartsWith("#/", StringComparison.Ordinal))
            {
                var p = njsonPath[2..].Replace("/", ".");
                return "$." + p;
            }
            if (njsonPath == "#")
            {
                return "$";
            }

            return njsonPath;
        }
    }
}
