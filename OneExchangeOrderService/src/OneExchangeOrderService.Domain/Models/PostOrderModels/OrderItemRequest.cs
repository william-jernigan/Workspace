using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Models.PostOrderModels
{
    [ExcludeFromCodeCoverage]
    public class OrderItemRequest
    {
        public string? ItemCode { get; set; }
        public string? Currency { get; set; }
        public decimal UnitPrice { get; set; }
        public string? TaxCurrency { get; set; }
        public decimal TaxAmount { get; set; }
        public List<OrderItemAttributesRequest>? Attributes { get; set; }
        public string? Memo { get; set; }
    }
}
