using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Models
{
    [ExcludeFromCodeCoverage]
    public class OrderResponse
    {
        public Guid? Id { get; set; }
        public string? OrderNumber { get; set; }
        public string? CancellationNumber { get; set; }
        public string? ExternalReferenceId { get; set; }
        public string? OrderStatus { get; set; }
        public decimal? Subtotal { get; set; }
        public decimal? OrderFee { get; set; }
        public decimal? CancellationFee { get; set; }
        public decimal? RefundPoints { get; set; }
        public string? SalesPersonFirstName { get; set; }
        public string? SalesPersonLastName { get; set; }
        public string? CustomerFirstName { get; set; }
        public string? CustomerLastName { get; set; }
        public string? CustomerExternalReferenceId { get; set; }
        public List<Guid>? ProfileId { get; set; }
        public DateTime? OrderDate { get; set; }
        public DateTime? CancelDate { get; set; }
        public string? SalesPersonEmailAddress { get; set; }
        public string? CustomerEmailAddress { get; set; }
        public string? CustomerPhoneNumber { get; set; }
        public decimal? TaxAmount { get; set; }
        public string? Memo { get; set; }

        public OrderItems? Item { get; set; }
        public List<GuestDetails>? Guests { get; set; }
        public bool IsUserEligibleForBookingFeeWaiver { get; set; }
        public List<ExchangeTransactionLogDetails>? ExchangeTransactions { get; set; }
        public List<OrderPaymentResponse>? OrderPayments { get; set; }

        public List<OrderRefundResponse>? OrderRefunds { get; set; }

    }
}
