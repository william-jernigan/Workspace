using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace OneExchangeOrderService.Domain.Models.Eventhub.OrderReservation
{
    [ExcludeFromCodeCoverage]
    public class OrderItemEventModelEvent
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("sequence")]
        public int Sequence { get; set; }

        [JsonPropertyName("itemId")]
        public Guid ItemId { get; set; }

        [JsonPropertyName("itemCode")]
        public string ItemCode { get; set; }

        [JsonPropertyName("itemDescription")]
        public string ItemDescription { get; set; }

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }

        [JsonPropertyName("unitPrice")]
        public decimal UnitPrice { get; set; }

        [JsonPropertyName("taxCurrency")]
        public string TaxCurrency { get; set; }

        [JsonPropertyName("taxAmount")]
        public decimal TaxAmount { get; set; }

        [JsonPropertyName("attributes")]
        public List<OrderItemAttributeEvent> Attributes { get; set; } = [];

        [JsonPropertyName("memo")]
        public string Memo { get; set; }
    }
}