using System.Text.Json.Serialization;

namespace OneExchangeOrderService.Domain.Models.V2.Reservation.Request
{
    public class PostalAddress
    {
        [JsonPropertyName("_sequence")] public int? Sequence { get; set; }
        [JsonPropertyName("addressType")] public string? AddressType { get; set; } // residence|business|other
        [JsonPropertyName("line1")] public string Line1 { get; set; } = default!;
        [JsonPropertyName("line2")] public string? Line2 { get; set; }
        [JsonPropertyName("city")] public string City { get; set; } = default!;
        [JsonPropertyName("state")] public string State { get; set; } = default!;
        [JsonPropertyName("postalCode")] public string PostalCode { get; set; } = default!;
        [JsonPropertyName("country")] public string Country { get; set; } = default!;
    }
}