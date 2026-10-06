using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Models
{
    [ExcludeFromCodeCoverage]
    public class OrderAttributeModel
    {
        public string? AttributeType { get; set; }
        public string? AttributeValue { get; set; }
    }
}
