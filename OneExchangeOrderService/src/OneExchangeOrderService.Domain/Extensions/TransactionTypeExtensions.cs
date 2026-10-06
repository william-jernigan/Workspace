using OneExchangeOrderService.Domain.Constants.Enums;

namespace OneExchangeOrderService.Domain.Extensions
{
    public static class TransactionTypeExtensions
    {
        public static string ToValue(this TransactionType type) => type switch
        {
            TransactionType.PurchaseByPoints => "PURCHASE_BY_POINTS",
            TransactionType.PointsRefund => "POINTS_REFUND",
            _ => throw new ArgumentOutOfRangeException(nameof(type), $"Unhandled transaction type: {type}")
        };

        public static TransactionType FromValue(string value) => value switch
        {
            "PURCHASE_BY_POINTS" => TransactionType.PurchaseByPoints,
            "POINTS_REFUND" => TransactionType.PointsRefund,
            _ => throw new ArgumentOutOfRangeException(nameof(value), $"Invalid transaction type: {value}")
        };
    }
}
