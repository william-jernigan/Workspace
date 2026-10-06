using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Models.Eventhub.BookingAlternateEmail
{
    [ExcludeFromCodeCoverage]
    public class OrderDetailsRequest
    {
        public string ConfirmationNumber { get; set; } = string.Empty;
    }
}
