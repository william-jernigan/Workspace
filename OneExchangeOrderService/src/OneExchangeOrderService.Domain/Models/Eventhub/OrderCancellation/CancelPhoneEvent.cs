using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
namespace OneExchangeOrderService.Domain.Models.Eventhub.OrderCancellation
{
    public class CancelPhoneEvent
    {
        [ExcludeFromCodeCoverage]
        [JsonPropertyName("number")]
        public string? Number { get; set; }
    }
}
