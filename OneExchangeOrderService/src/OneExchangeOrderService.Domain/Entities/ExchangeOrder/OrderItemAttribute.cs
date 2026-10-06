using OneExchangeOrderService.Domain.Entities.Common;
using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Entities.ExchangeOrder
{
    [ExcludeFromCodeCoverage]
    [SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix",
        Justification = "Matches database table name exch_order.orderItemAttribute")]
    public partial class OrderItemAttribute : BaseEntity
    {

        public Guid TenantId { get; set; }

        public Guid OrderItemId { get; set; }

        public string AttributeType { get; set; } = null!;

        public string AttributeValue { get; set; } = null!;

        public int Sequence { get; set; }

        public virtual OrderItem OrderItem { get; set; } = null!;
    }
}
