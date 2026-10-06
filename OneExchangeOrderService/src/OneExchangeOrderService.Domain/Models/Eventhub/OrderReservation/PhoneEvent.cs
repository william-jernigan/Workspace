using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace OneExchangeOrderService.Domain.Models.Eventhub.OrderReservation
{
    [ExcludeFromCodeCoverage]
    public class PhoneEvent
    {
        [JsonPropertyName("sequence")]
        public int? Sequence { get; set; }

        [JsonPropertyName("phoneType")]
        public string? PhoneType { get; set; }

        [JsonPropertyName("phonePurpose")]
        public string? PhonePurpose { get; set; }

        [JsonPropertyName("number")]
        public string? Number { get; set; }

        [JsonPropertyName("extension")]
        public string? Extension { get; set; }

        [JsonPropertyName("verifiedDate")]
        public DateTime? VerifiedDate { get; set; }
    }
}
