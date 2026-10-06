using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Models.OrderDetails
{
    [ExcludeFromCodeCoverage]
    public class OrderItemAttributeResponse : BaseResponse
    {
        public Guid TenantId { get; set; }

        public Guid OrderItemId { get; set; }

        public string AttributeType { get; set; } = null!;

        public string AttributeValue { get; set; } = null!;

        public int Sequence { get; set; }

        public virtual OrderItemResponse OrderItem { get; set; } = null!;
    }
}
