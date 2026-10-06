using OneExchangeOrderService.Domain.Entities.Common;
using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Entities.Exchange
{
    [ExcludeFromCodeCoverage]
    public partial class Program : BaseEntity
    {
        public Guid TenantId { get; set; }

        public string ProgramCode { get; set; } = null!;

        public string ProgramName { get; set; } = null!;

        public virtual Tenant Tenant { get; set; } = null!;

        public virtual ICollection<TradingAgreement> TradingAgreements { get; set; } = [];
    }
}
