using OneExchangeOrderService.Domain.Constants.Enums;
using System.Globalization;

namespace OneExchangeOrderService.Domain.Extensions
{
    public static class TransactionAmountFormatExtensions
    {
        public static string FormatAmount(this TransactionAmountFormat format, decimal amount) => format switch
        {
            TransactionAmountFormat.NegativeF5 => amount.ToString("#,0;(#,0);(0)", CultureInfo.InvariantCulture),
            TransactionAmountFormat.DefaultF4 => amount.ToString("0", CultureInfo.InvariantCulture),
            _ => amount.ToString("0", CultureInfo.InvariantCulture)
        };
    }
}