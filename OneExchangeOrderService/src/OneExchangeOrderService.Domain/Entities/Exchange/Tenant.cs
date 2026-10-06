using OneExchangeOrderService.Domain.Entities.Common;
using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Entities.Exchange
{
    [ExcludeFromCodeCoverage]
    public partial class Tenant : BaseEntity
    {

        public Guid OrganizationId { get; set; }

        public string TenantCode { get; set; } = null!;

        public DateTimeOffset ContractStartDate { get; set; }

        public DateTimeOffset? ContractEndDate { get; set; }

        public virtual Organization Organization { get; set; } = null!;

        public virtual ICollection<Partner> Partners { get; set; } = [];

        public virtual ICollection<Program> Programs { get; set; } = [];

        public virtual ICollection<TradingAgreement> TradingAgreements { get; set; } = [];
    }
}
