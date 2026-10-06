using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Models
{
    [ExcludeFromCodeCoverage]
    public class CurrencyEquivalent
    {
        public string? AmountType { get; set; }
        public string? Currency { get; set; }
        public decimal? Amount { get; set; }
    }
}
