using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace OneExchangeOrderService.Domain.Models.Eventhub.OrderReservation
{
    [ExcludeFromCodeCoverage]
    public class ExternalOrderReferenceEvent
    {
        [JsonPropertyName("externalSourceSystem")]
        public string? ExternalSourceSystem { get; set; }

        [JsonPropertyName("externalReferenceId")]
        public string? ExternalReferenceId { get; set; } = string.Empty;
    }
}
