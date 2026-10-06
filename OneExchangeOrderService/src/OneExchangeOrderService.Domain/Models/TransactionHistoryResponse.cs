
using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Models
{
    [ExcludeFromCodeCoverage]
    public class TransactionHistoryResponse
    {
        public string? Resort { get; set; }
        public string? PointUsed { get; set; }
        public DateTime ConfirmationDate { get; set; }
        public string? ConfirmationNumber { get; set; }
        public string Currency { get; set; } = "Points";
        public string Participant { get; set; } = "BXG";
        public Guid OrderId { get; set; }
        public string CustomerExternalReferenceId { get; set; }
    }
}
