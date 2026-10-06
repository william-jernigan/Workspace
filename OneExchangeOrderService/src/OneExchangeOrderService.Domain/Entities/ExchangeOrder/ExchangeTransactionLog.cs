using OneExchangeOrderService.Domain.Entities.Common;
using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Entities.ExchangeOrder
{
    [ExcludeFromCodeCoverage]
    public partial class ExchangeTransactionLog : BaseEntity
    {
        public Guid TenantId { get; set; }

        public Guid OrderId { get; set; }

        public DateTimeOffset TransactionDate { get; set; }

        public string TransactionType { get; set; } = null!;

        public string? FromAccountId { get; set; }

        public string? FromCurrency { get; set; }

        public decimal FromTransactionAmount { get; set; }

        public string? FromConfirmationNumber { get; set; }

        public string? ToAccountId { get; set; }

        public string? ToCurrency { get; set; }

        public decimal ToTransactionAmount { get; set; }

        public string? ToConfirmationNumber { get; set; }

        public Guid? TransactionReferenceId { get; set; }

        public string? Memo { get; set; }

        public virtual Order Order { get; set; } = null!;
    }
}
