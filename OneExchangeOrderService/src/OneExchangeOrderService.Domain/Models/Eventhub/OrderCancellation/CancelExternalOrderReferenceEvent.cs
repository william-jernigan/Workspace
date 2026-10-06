using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace OneExchangeOrderService.Domain.Models.Eventhub.OrderCancellation
{
    [ExcludeFromCodeCoverage]
    public class CancelExternalOrderReferenceEvent
    {
        [JsonPropertyName("externalSourceSystem")]
        public string? ExternalSourceSystem { get; set; }

        [JsonPropertyName("externalReferenceId")]
        public string? ExternalReferenceId { get; set; } = string.Empty;
    }
}
