using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;


namespace OneExchangeOrderService.Domain.Models.Eventhub.OrderCancellation
{
    [ExcludeFromCodeCoverage]
    public class CancelOrderItemAttributeEvent
    {
        [JsonPropertyName("attributeType")]
        public string? AttributeType { get; set; }

        [JsonPropertyName("attributeValue")]
        public string? AttributeValue { get; set; }
    }
}
