using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace OneExchangeOrderService.Domain.Models.PostOrderModels
{
    [ExcludeFromCodeCoverage]
    public class DigitalAddressRequest
    {
        public int? Sequence { get; set; }
        public string? DigitalAddressType { get; set; }

        [JsonPropertyName("digitalAddress")]
        public string? DigitalAddressDetail { get; set; }
        public DateTime? VerifiedDate { get; set; }
    }
}
