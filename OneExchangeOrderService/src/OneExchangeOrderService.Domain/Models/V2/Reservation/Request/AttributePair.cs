using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace OneExchangeOrderService.Domain.Models.V2.Reservation.Request
{
    [ExcludeFromCodeCoverage]
    public class AttributePair
    {
        [JsonPropertyName("attribute")] public string Attribute { get; set; } = default!;
        [JsonPropertyName("value")] public string Value { get; set; } = default!;
    }
}