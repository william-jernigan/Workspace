using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Models.PostOrderModels
{
    [ExcludeFromCodeCoverage]
    public class ProfileRequest
    {
        public string? ProfileType { get; set; }
        public string? ExternalSourceSystem { get; set; }
        public string? ExternalReferenceId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public List<AddressRequest>? Addresses { get; set; }
        public List<PhoneRequest>? Phones { get; set; }
        public List<DigitalAddressRequest>? DigitalAddresses { get; set; }
        public List<IdentifierAttributesRequest>? Identifiers { get; set; }
    }
}