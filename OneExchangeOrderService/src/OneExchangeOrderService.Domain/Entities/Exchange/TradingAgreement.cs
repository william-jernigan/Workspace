using OneExchangeOrderService.Domain.Entities.Common;
using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Entities.Exchange
{
    [ExcludeFromCodeCoverage]
    public partial class TradingAgreement : BaseEntity
    {

        public Guid TenantId { get; set; }

        public Guid ProgramId { get; set; }

        public Guid SupplierPartnerId { get; set; }

        public Guid BuyerPartnerId { get; set; }

        public string TradingAgreementCode { get; set; } = null!;

        public DateTimeOffset EffectiveStartDate { get; set; }

        public DateTimeOffset? EffectiveEndDate { get; set; }

        public virtual Partner BuyerPartner { get; set; } = null!;

        public virtual Program Program { get; set; } = null!;

        public virtual Partner SupplierPartner { get; set; } = null!;

        public virtual Tenant Tenant { get; set; } = null!;
    }
}
