using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace OneExchangeOrderService.Domain.Models.Eventhub.OrderCancellation
{
    [ExcludeFromCodeCoverage]
    public class CancelOrderEvent
    {
        [JsonPropertyName("source")]
        public string Source { get; set; } = "OneXChange";

        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("specversion")]
        public string SpecVersion { get; set; } = "1.0";

        [JsonPropertyName("type")]
        public string Type { get; set; } = "Order-Cancellation";

        [JsonPropertyName("datacontenttype")]
        public string DataContentType { get; set; } = "application/json";

        [JsonPropertyName("dataschema")]
        public string DataSchema { get; set; }

        [JsonPropertyName("time")]
        public DateTime Time { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("ispartial")]
        public bool IsPartial { get; set; } = false;

        [JsonPropertyName("data")]
        public CancelOrderEventhubData Data { get; set; }
    }
}
