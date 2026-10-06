using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Models.UpdateOrder.Request
{
    [ExcludeFromCodeCoverage]
    public class UpdateOrderRequest
    {
        // TODO To be inline with HGV repos api layer request and response objects should be present in DTO project
        public string OrderStatus { get; set; }
        public string Memo { get; set; }
        public ExternalOrderReference ExternalOrderReference { get; set; }
        public List<PaymentRequest> Payments { get; set; }
        public ExchangeTransactionRequest ExchangeTransaction { get; set; }
    }
}
