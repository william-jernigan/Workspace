using OneExchangeOrderService.Domain.Entities.Common;
using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Entities.ExchangeOrder
{
    [ExcludeFromCodeCoverage]
    public partial class OrderPayment : BaseEntity
    {

        public Guid TenantId { get; set; }

        public Guid OrderId { get; set; }

        public string PaymentPurpose { get; set; } = null!;

        public string? Currency { get; set; }

        public decimal Amount { get; set; }

        public DateTime PaymentDate { get; set; }

        public string? PaymentAccountToken { get; set; }

        public virtual Order Order { get; set; } = null!;

        public virtual ICollection<OrderRefund> OrderRefunds { get; set; } = [];
    }
}
