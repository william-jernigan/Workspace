using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Models.PostOrderModels
{
    [ExcludeFromCodeCoverage]
    public class AddressRequest
    {

        public int? Sequence { get; set; }
        public string? AddressType { get; set; }
        public string? AddressPurpose { get; set; }
        public string? Line1 { get; set; }
        public string? Line2 { get; set; }
        public string? Line3 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Province { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public DateTime? VerifiedDate { get; set; }
        public bool PostalServiceVerified { get; set; }
    }
}
