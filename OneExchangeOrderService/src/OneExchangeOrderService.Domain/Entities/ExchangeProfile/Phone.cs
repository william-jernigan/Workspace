using OneExchangeOrderService.Domain.Entities.Common;
using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Entities.ExchangeProfile
{
    [ExcludeFromCodeCoverage]
    public partial class Phone : BaseEntity
    {

        public Guid ProfileId { get; set; }

        public int Sequence { get; set; }

        public string? PhoneType { get; set; }

        public string? PhonePurpose { get; set; }

        public string? Number { get; set; }

        public string? Extension { get; set; }

        public DateTime? VerifiedDate { get; set; }

        public bool NotForMarketing { get; set; }

        public virtual Profile Profile { get; set; } = null!;
    }
}
