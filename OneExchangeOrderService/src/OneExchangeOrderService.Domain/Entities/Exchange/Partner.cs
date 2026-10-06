using OneExchangeOrderService.Domain.Entities.Common;
using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Entities.Exchange
{
    [ExcludeFromCodeCoverage]
    public partial class Partner : BaseEntity
    {

        public Guid TenantId { get; set; }

        public Guid OrganizationId { get; set; }

        public string PartnerCode { get; set; } = null!;

        public string? DefaultPointCurrencyCode { get; set; }

        public virtual Organization Organization { get; set; } = null!;

        public virtual Tenant Tenant { get; set; } = null!;

        public virtual ICollection<TradingAgreement> TradingAgreementBuyerPartners { get; set; } = [];

        public virtual ICollection<TradingAgreement> TradingAgreementSupplierPartners { get; set; } = [];
    }
}
