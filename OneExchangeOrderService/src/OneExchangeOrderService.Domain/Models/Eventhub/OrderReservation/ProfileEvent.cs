using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace OneExchangeOrderService.Domain.Models.Eventhub.OrderReservation
{
    [ExcludeFromCodeCoverage]
    public class ProfileEvent
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
        public List<AddressEvent>? Addresses { get; set; }

        [JsonPropertyName("phones")]
        public List<PhoneEvent>? Phones { get; set; }

        [JsonPropertyName("digitalAddresses")]
        public List<DigitalAddressEvent>? DigitalAddresses { get; set; }

        [JsonPropertyName("identifiers")]
        public List<IdentifierAttributesEvent>? Identifiers { get; set; }
    }
}
