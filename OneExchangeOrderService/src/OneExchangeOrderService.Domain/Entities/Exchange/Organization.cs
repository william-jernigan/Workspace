using OneExchangeOrderService.Domain.Entities.Common;
using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Entities.Exchange
{
    [ExcludeFromCodeCoverage]
    public partial class Organization : BaseEntity
    {

        public Guid ProfileId { get; set; }

        public string? OrganizationName { get; set; }

        public string? OrganizationSuffix { get; set; }

        public virtual ICollection<Partner> Partners { get; set; } = [];

        public virtual ICollection<Tenant> Tenants { get; set; } = [];
    }
}
