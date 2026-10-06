using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace OneExchangeOrderService.Domain.Models.Eventhub.OrderCancellation
{
    [ExcludeFromCodeCoverage]
    public class CancelProfileEvent
    {
        [JsonPropertyName("profileType")]
        public string? ProfileType { get; set; }

        [JsonPropertyName("externalSourceSystem")]
        public string? ExternalSourceSystem { get; set; }

        [JsonPropertyName("externalReferenceId")]
        public string? ExternalReferenceId { get; set; }

        [JsonPropertyName("firstName")]
        public string? FirstName { get; set; }

        [JsonPropertyName("lastName")]
        public string? LastName { get; set; }

        [JsonPropertyName("addresses")]
        public List<CancelAddressEvent>? Addresses { get; set; }

        [JsonPropertyName("phones")]
        public List<CancelPhoneEvent>? Phones { get; set; }

        [JsonPropertyName("digitalAddresses")]
        public List<CancelDigitalAddressEvent>? DigitalAddresses { get; set; }

        [JsonPropertyName("identifiers")]
        public List<CancelIdentifierAttributesEvent>? Identifiers { get; set; }

    }
}
