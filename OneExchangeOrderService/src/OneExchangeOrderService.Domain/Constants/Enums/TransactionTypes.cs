using System.ComponentModel;

namespace OneExchangeOrderService.Domain.Constants.Enums
{
    public enum TransactionType
    {
        [Description("PurchasedByPoints")]
        PurchaseByPoints,
        [Description("PointsRefund")]
        PointsRefund
    }
}
