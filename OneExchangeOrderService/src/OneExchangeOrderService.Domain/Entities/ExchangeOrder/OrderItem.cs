using OneExchangeOrderService.Domain.Entities.Common;
using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Entities.ExchangeOrder
{
    [ExcludeFromCodeCoverage]
    public partial class OrderItem : BaseEntity
    {

        public Guid TenantId { get; set; }

        public Guid OrderId { get; set; }

        public Guid ItemId { get; set; }

        public string ItemCode { get; set; } = null!;

        public int Sequence { get; set; }

        public string ItemDescription { get; set; } = null!;

        public decimal Quantity { get; set; }

        public string? UnitPriceCurrency { get; set; }

        public decimal UnitPrice { get; set; }

        public string TaxCurrency { get; set; } = null!;

        public decimal TaxAmount { get; set; }

        public string? Memo { get; set; }

        public string? ExtendedReference { get; set; }

        public virtual Order Order { get; set; } = null!;

        public virtual ICollection<OrderItemAttribute> OrderItemAttributes { get; set; } = [];
    }
}
