using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace OneExchangeOrderService.Domain.Models.Eventhub.OrderReservation
{
    [ExcludeFromCodeCoverage]
    public class IdentifierAttributesEvent
    {
        [JsonPropertyName("identifierType")]
        public string? IdentifierType { get; set; }

        [JsonPropertyName("identifierSource")]
        public string? IdentifierSource { get; set; }

        [JsonPropertyName("identifier")]
        public string? Identifier { get; set; }
    }
}
