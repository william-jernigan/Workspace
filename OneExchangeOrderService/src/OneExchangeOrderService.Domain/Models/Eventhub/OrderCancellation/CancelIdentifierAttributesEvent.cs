using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace OneExchangeOrderService.Domain.Models.Eventhub.OrderCancellation
{
    [ExcludeFromCodeCoverage]
    public class CancelIdentifierAttributesEvent
    {
        [JsonPropertyName("identifierType")]
        public string? IdentifierType { get; set; }

        [JsonPropertyName("identifierSource")]
        public string? IdentifierSource { get; set; }

        [JsonPropertyName("identifier")]
        public string? Identifier { get; set; }
    }
}
