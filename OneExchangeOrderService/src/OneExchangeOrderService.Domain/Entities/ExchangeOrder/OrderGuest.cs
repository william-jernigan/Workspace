using OneExchangeOrderService.Domain.Entities.Common;
using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Entities.ExchangeOrder
{
    [ExcludeFromCodeCoverage]
    public partial class OrderGuest : BaseEntity
    {

        public Guid TenantId { get; set; }

        public Guid OrderId { get; set; }

        public Guid ProfileId { get; set; }

        public bool IsPrimary { get; set; }

        public virtual Order Order { get; set; } = null!;
    }
}
