using OneExchangeOrderService.Domain.Constants.Enums;

namespace OneExchangeOrderService.Domain.Extensions
{
    public static class OrderStatusExtensions
    {
        public static List<string> GetNonEligibleStatuses() =>
            [
                OrderStatus.CANCELLED.ToString(),
                OrderStatus.REFUNDED.ToString()
            ];
    }
}