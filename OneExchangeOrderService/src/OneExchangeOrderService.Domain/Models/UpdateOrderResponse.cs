using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Models
{
    [ExcludeFromCodeCoverage]
    public class UpdateOrderResponse
    {
        public string OrderNumber { get; set; }
        public string OrderId { get; set; }
        public string OrderStatus { get; set; }
        public string Message { get; set; }
        public bool IsUpdatedSuccessfully { get; set; }
    }
}
