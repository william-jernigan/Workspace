using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Models.UpdateOrder.Request
{
    [ExcludeFromCodeCoverage]
    public class PaymentRequest
    {
        public string PaymentPurpose { get; set; }
        public string Currency { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentAccountToken { get; set; }
    }
}
