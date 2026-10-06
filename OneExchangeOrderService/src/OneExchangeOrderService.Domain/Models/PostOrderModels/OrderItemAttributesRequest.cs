using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Models.PostOrderModels
{
    [ExcludeFromCodeCoverage]
    public class OrderItemAttributesRequest
    {
        public string? AttributeType { get; set; }
        public string? AttributeValue { get; set; }
    }
}
