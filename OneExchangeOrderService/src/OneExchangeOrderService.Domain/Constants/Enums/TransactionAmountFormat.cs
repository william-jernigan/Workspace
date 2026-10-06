using System.ComponentModel;

namespace OneExchangeOrderService.Domain.Constants.Enums
{
    public enum TransactionAmountFormat
    {
        [Description("F4")]
        DefaultF4 = 0,

        [Description("F5")]
        NegativeF5 = 1
    }
}