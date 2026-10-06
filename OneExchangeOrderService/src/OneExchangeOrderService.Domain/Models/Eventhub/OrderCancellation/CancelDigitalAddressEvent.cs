using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace OneExchangeOrderService.Domain.Models.Eventhub.OrderCancellation
{
    [ExcludeFromCodeCoverage]
    public class CancelDigitalAddressEvent
    {
        [JsonPropertyName("sequence")]
        public int? Sequence { get; set; }
        [JsonPropertyName("digitalAddressType")]
        public string? DigitalAddressType { get; set; }

        [JsonPropertyName("digitalAddress")]
        public string? DigitalAddress { get; set; }
    }
}
