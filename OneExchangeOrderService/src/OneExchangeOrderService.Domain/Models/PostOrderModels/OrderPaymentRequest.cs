using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Models.PostOrderModels
{
    [ExcludeFromCodeCoverage]
    public class OrderPaymentRequest
    {
        public Guid? Id { get; set; }
        public string? PaymentPurpose { get; set; }
        public string? Currency { get; set; }
        public decimal? Amount { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string? PaymentAccountToken { get; set; }
        public string? ExpirationDate { get; set; }
    }
}
