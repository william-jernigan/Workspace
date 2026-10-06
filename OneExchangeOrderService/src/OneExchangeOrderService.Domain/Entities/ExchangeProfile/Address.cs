using OneExchangeOrderService.Domain.Entities.Common;
using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Entities.ExchangeProfile
{
    [ExcludeFromCodeCoverage]
    public partial class Address : BaseEntity
    {

        public Guid ProfileId { get; set; }

        public int Sequence { get; set; }

        public string? AddressType { get; set; }

        public string? AddressPurpose { get; set; }

        public string? Line1 { get; set; }

        public string? Line2 { get; set; }

        public string? Line3 { get; set; }

        public string? City { get; set; }

        public string? County { get; set; }

        public string? State { get; set; }

        public string? Province { get; set; }

        public string? Postalcode { get; set; }

        public string? Country { get; set; }

        public DateTime? VerifiedDate { get; set; }

        public bool PostalServiceVerified { get; set; }

        public string? ExtendedReference { get; set; }

        public bool NotForMarketing { get; set; }

        public virtual Profile Profile { get; set; } = null!;
    }
}
