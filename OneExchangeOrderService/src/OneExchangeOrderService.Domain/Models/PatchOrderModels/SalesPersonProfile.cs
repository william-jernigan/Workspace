using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace OneExchangeOrderService.Domain.Models.PatchOrderModels
{
    [ExcludeFromCodeCoverage]
    public class SalesPersonProfile
    {
        [JsonPropertyName("profileType")]
        public string? ProfileType { get; set; }

        [JsonPropertyName("firstName")]
        public string? FirstName { get; set; }

        [JsonPropertyName("lastName")]
        public string? LastName { get; set; }

        [JsonPropertyName("digitalAddresses")]
        public List<DigitalAddress>? DigitalAddresses { get; set; }
    }
}
