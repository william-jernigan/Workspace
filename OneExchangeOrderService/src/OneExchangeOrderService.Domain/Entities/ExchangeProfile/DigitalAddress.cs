using OneExchangeOrderService.Domain.Entities.Common;
using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Entities.ExchangeProfile
{
    [ExcludeFromCodeCoverage]
    public partial class DigitalAddress : BaseEntity
    {

        public Guid ProfileId { get; set; }

        public int Sequence { get; set; }

        public string? DigitalAddressType { get; set; }

        public string? DigitalAddress1 { get; set; }

        public DateTime? VerifiedDate { get; set; }

        public bool NotForMarketing { get; set; }

        public virtual Profile Profile { get; set; } = null!;
    }
}
