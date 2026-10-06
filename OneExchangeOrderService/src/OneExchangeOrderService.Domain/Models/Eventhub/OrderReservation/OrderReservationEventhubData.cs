using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace OneExchangeOrderService.Domain.Models.Eventhub.OrderReservation
{
    [ExcludeFromCodeCoverage]
    public class OrderReservationEventhubData
    {
        [JsonPropertyName("systemMeta")]
        public SystemMetaEvent SystemMeta { get; set; } = new();

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

        [JsonPropertyName("currencyEquivalent")]
        public List<CurrencyEquivalent>? CurrencyEquivalent { get; set; }

        [JsonPropertyName("memo")]
        public string Memo { get; set; }

        [JsonPropertyName("externalOrderReference")]
        public ExternalOrderReferenceEvent ExternalOrderReference { get; set; }

        [JsonPropertyName("customerProfile")]
        public ProfileEvent CustomerProfile { get; set; }

        [JsonPropertyName("salesPersonProfile")]
        public ProfileEvent SalesPersonProfile { get; set; }

        [JsonPropertyName("guests")]
        public List<ProfileEvent> Guests { get; set; } = [];

        [JsonPropertyName("orderItems")]
        public List<OrderItemEventModelEvent> OrderItems { get; set; } = [];

        [JsonPropertyName("payments")]
        public List<OrderPaymentEvent> Payments { get; set; } = [];

        [JsonPropertyName("orderReturn")]
        public OrderReservationReturn OrderReturn { get; set; } = new();

        [JsonPropertyName("exchangeTransaction")]
        public List<ExchangeTransaction> ExchangeTransactions { get; set; } = [];
        [JsonPropertyName("waiveFee")]
        public bool WaiveFee { get; set; }
    }
}
