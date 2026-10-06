using System.ComponentModel;

namespace OneExchangeOrderService.Domain.Constants.Enums
{
    public enum OrderStatus
    {
        [Description("Order status : PENDING")]
        PENDING,
        [Description("Order status : CONFIRMED")]
        CONFIRMED,
        [Description("Order status : CANCELLED")]
        CANCELLED,
        [Description("Order status : REFUNDED")]
        REFUNDED

    }
}
