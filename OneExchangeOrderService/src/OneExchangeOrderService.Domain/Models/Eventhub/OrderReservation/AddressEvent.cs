using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace OneExchangeOrderService.Domain.Models.Eventhub.OrderReservation
{
    [ExcludeFromCodeCoverage]
    public class AddressEvent
    {
        [JsonPropertyName("sequence")]
        public int? Sequence { get; set; }

        [JsonPropertyName("addressType")]
        public string? AddressType { get; set; }

        [JsonPropertyName("addressPurpose")]
        public string? AddressPurpose { get; set; }

        [JsonPropertyName("line1")]
        public string? Line1 { get; set; }

        [JsonPropertyName("line2")]
        public string? Line2 { get; set; }

        [JsonPropertyName("line3")]
        public string? Line3 { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("province")]
        public string? Province { get; set; }

        [JsonPropertyName("postalCode")]
        public string? PostalCode { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("verifiedDate")]
        public DateTime? VerifiedDate { get; set; }
    }
}
