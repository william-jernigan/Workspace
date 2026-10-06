using System.Text.Json.Serialization;

namespace OneExchangeOrderService.Domain.Models.V2.Reservation.Request
{
    public class PhoneNumber
    {
        [JsonPropertyName("_sequence")] public int? Sequence { get; set; }
        [JsonPropertyName("phoneType")] public string PhoneType { get; set; } = default!; // mobile|home|work
        [JsonPropertyName("number")] public string Number { get; set; } = default!;
    }
}