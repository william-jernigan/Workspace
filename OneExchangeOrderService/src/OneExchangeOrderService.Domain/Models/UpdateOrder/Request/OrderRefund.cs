using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Models.UpdateOrder.Request
{
    [ExcludeFromCodeCoverage]
    public class OrderRefund
    {
        public string RefundPurpose { get; set; }
        public string Currency { get; set; }
        public decimal Amount { get; set; }
        public Guid OriginalPaymentId { get; set; }
    }
}
