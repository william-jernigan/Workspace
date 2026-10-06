using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace OneExchangeOrderService.Domain.Models.Eventhub.OrderReservation
{
    [ExcludeFromCodeCoverage]
    public class OrderItemAttributeEvent
    {
        [JsonPropertyName("attributeType")]
        public string? AttributeType { get; set; }

        [JsonPropertyName("attributeValue")]
        public string? AttributeValue { get; set; }
    }
}
