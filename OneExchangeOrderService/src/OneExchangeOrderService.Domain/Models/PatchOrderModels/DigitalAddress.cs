using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace OneExchangeOrderService.Domain.Models.PatchOrderModels
{
    [ExcludeFromCodeCoverage]
    public class DigitalAddress
    {
        [JsonPropertyName("digitalAddressType")]
        public string? DigitalAddressType { get; set; }

        [JsonPropertyName("digitalAddress")]
        public string? Address { get; set; }
    }
}
