using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace OneExchangeOrderService.Domain.Models.Eventhub.OrderReservation
{
    [ExcludeFromCodeCoverage]
    public class DigitalAddressEvent
    {
        [JsonPropertyName("sequence")]
        public int? Sequence { get; set; }
        [JsonPropertyName("digitalAddressType")]
        public string? DigitalAddressType { get; set; }

        [JsonPropertyName("digitalAddress")]
        public string? DigitalAddress { get; set; }
        [JsonPropertyName("verifiedDate")]
        public DateTime? VerifiedDate { get; set; }
    }
}
