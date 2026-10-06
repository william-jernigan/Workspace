using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Models.OrderDetails
{
    [ExcludeFromCodeCoverage]
    public class OrderItemResponse : BaseResponse
    {

        public Guid TenantId { get; set; }

        public Guid OrderId { get; set; }

        public Guid ItemId { get; set; }

        public string ItemCode { get; set; } = null!;

        public int Sequence { get; set; }

        public string ItemDescription { get; set; } = null!;

        public decimal Quantity { get; set; }

        public string? UnitPriceCurrency { get; set; }

        public decimal UnitPrice { get; set; }

        public string TaxCurrency { get; set; } = null!;

        public decimal TaxAmount { get; set; }

        public string? Memo { get; set; }

        public string? ExtendedReference { get; set; }

        public virtual OrderDataResponse Order { get; set; } = null!;

        public virtual List<OrderItemAttributeResponse> OrderItemAttributes { get; set; } = [];
    }
}
