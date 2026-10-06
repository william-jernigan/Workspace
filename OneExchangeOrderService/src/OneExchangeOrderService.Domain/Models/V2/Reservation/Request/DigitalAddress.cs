using System.Text.Json.Serialization;

namespace OneExchangeOrderService.Domain.Models.V2.Reservation.Request
{
    public class DigitalAddress
    {
        [JsonPropertyName("_sequence")] public int? Sequence { get; set; }
        [JsonPropertyName("digitalAddressType")] public string DigitalAddressType { get; set; } = default!; // email
        [JsonPropertyName("digitalAddress")] public string DigitalAddressValue { get; set; } = default!;
    }
}