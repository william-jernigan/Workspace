using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace OneExchangeOrderService.Domain.Models.V2.Reservation.Request
{
    [ExcludeFromCodeCoverage]
    public class GuestCounts
    {
        [JsonPropertyName("adults")] public int? Adults { get; set; }
        [JsonPropertyName("children")] public int? Children { get; set; }
    }
}