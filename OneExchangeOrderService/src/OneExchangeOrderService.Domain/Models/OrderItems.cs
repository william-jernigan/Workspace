using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Models
{
    [ExcludeFromCodeCoverage]
    public class OrderItems
    {
        public string? ItemDescription { get; set; }
        public string? ItemCode { get; set; }
        public List<OrderAttributeModel>? Attributes { get; set; }
    }
}
