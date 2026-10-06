using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace OneExchangeOrderService.Domain.Models.Eventhub.OrderReservation
{
    [ExcludeFromCodeCoverage]
    public class ExchangeTransaction
    {
        [JsonPropertyName("transactionDate")]
        public DateTimeOffset? TransactionDate { get; set; }

        [JsonPropertyName("transactionType")]
        public string? TransactionType { get; set; }

        [JsonPropertyName("fromAccountId")]
        public string? FromAccountId { get; set; }

        [JsonPropertyName("fromCurrency")]
        public string? FromCurrency { get; set; }

        [JsonPropertyName("fromTransactionAmount")]
        public decimal? FromTransactionAmount { get; set; }

        [JsonPropertyName("toAccountId")]
        public string? ToAccountId { get; set; }

        [JsonPropertyName("toCurrency")]
        public string? ToCurrency { get; set; }

        [JsonPropertyName("toTransactionAmount")]
        public decimal? ToTransactionAmount { get; set; }

        [JsonPropertyName("transactionReferenceId")]
        public Guid? TransactionReferenceId { get; set; }

        [JsonPropertyName("memo")]
        public string? Memo { get; set; }
    }
}
