using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Models.UpdateOrder.Request
{
    [ExcludeFromCodeCoverage]
    public class OrderPatchModel
    {
        public string OrderStatus { get; set; }
        public string Memo { get; set; }
        public ExternalOrderReference ExternalOrderReference { get; set; }
        public OrderReturn OrderReturn { get; set; }
        public List<OrderRefund> OrderRefund { get; set; }
        public List<Address> Addresses { get; set; }
        public List<PaymentRequest> Payments { get; set; }
        public List<ExchangeTransactionRequest> ExchangeTransactions { get; set; }
        public string StatusReason { get; set; }
        public List<OrderItemAttributeRequest> OrderItemAttributes { get; set; } = [];
    }
}
