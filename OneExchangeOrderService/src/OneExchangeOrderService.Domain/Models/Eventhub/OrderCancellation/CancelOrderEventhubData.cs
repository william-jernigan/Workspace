using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace OneExchangeOrderService.Domain.Models.Eventhub.OrderCancellation
{
    [ExcludeFromCodeCoverage]
    public class CancelOrderEventhubData
    {
        [JsonPropertyName("systemMeta")]
        public CancelSystemMetaEvent SystemMeta { get; set; } = new();

        [JsonPropertyName("orderId")]
        public string OrderId { get; set; }

        [JsonPropertyName("tradingAgreementCode")]
        public string TradingAgreementCode { get; set; }

        [JsonPropertyName("orderType")]
        public string OrderType { get; set; }

        [JsonPropertyName("orderNumber")]
        public string OrderNumber { get; set; }

        [JsonPropertyName("orderStatus")]
        public string OrderStatus { get; set; }

        [JsonPropertyName("orderDate")]
        public DateTime? OrderDate { get; set; }

        [JsonPropertyName("cancelDate")]
        public DateTime? CancelDate { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }

        [JsonPropertyName("subtotal")]
        public decimal Subtotal { get; set; }

        [JsonPropertyName("taxCurrency")]
        public string TaxCurrency { get; set; }

        [JsonPropertyName("taxAmount")]
        public decimal TaxAmount { get; set; }

        [JsonPropertyName("feeCurrency")]
        public string FeeCurrency { get; set; }

        [JsonPropertyName("orderFee")]
        public decimal OrderFee { get; set; }

        [JsonPropertyName("memo")]
        public string Memo { get; set; }

        [JsonPropertyName("addresses")]
        public List<Address> Addresses { get; set; }

        [JsonPropertyName("externalOrderReference")]
        public CancelExternalOrderReferenceEvent ExternalOrderReference { get; set; }

        [JsonPropertyName("customerProfile")]
        public CancelProfileEvent CustomerProfile { get; set; }

        [JsonPropertyName("salesPersonProfile")]
        public CancelProfileEvent SalesPersonProfile { get; set; }

        [JsonPropertyName("guests")]
        public List<CancelProfileEvent> Guests { get; set; } = [];

        [JsonPropertyName("orderItems")]
        public List<CancelOrderItemEventModelEvent> OrderItems { get; set; } = [];

        [JsonPropertyName("payments")]
        public List<CancelOrderPayment> Payments { get; set; } = [];

        [JsonPropertyName("orderReturn")]
        public CancelOrderReservationReturn OrderReturn { get; set; } = new();

        [JsonPropertyName("exchangeTransactions")]
        public List<CancelExchangeTransaction> ExchangeTransactions { get; set; } = [];

        [JsonPropertyName("isImpersonationExpiredMember")]
        public bool? IsImpersonationExpiredMember { get; set; } = false;
    }
}
