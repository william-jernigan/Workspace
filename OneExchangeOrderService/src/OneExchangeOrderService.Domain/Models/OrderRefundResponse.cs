using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Models
{
    [ExcludeFromCodeCoverage]
    public class OrderRefundResponse
    {
        public Guid TenantId { get; set; }

        public Guid? OrderReturnId { get; set; }

        public string RefundPurpose { get; set; } = null!;

        public Guid OriginalPaymentId { get; set; }

        public string? SalesPersonEmail { get; set; }

        public string? SalesPersonFirstName { get; set; }

        public string? SalesPersonLastName { get; set; }

        public string? Currency { get; set; }

        public decimal Amount { get; set; }

        public DateTime? RefundDate { get; set; }

        public string? RefundAccountToken { get; set; }
    }
}
