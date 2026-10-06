using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Models
{
    [ExcludeFromCodeCoverage]
    public partial class OrderPaymentResponse
    {
        public Guid Id { get; set; }

        public Guid TenantId { get; set; }

        public Guid OrderId { get; set; }

        public string PaymentPurpose { get; set; } = null!;

        public string? Currency { get; set; }

        public decimal Amount { get; set; }

        public DateTime PaymentDate { get; set; }

        public string? PaymentAccountToken { get; set; }

    }
}
