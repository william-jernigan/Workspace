using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace OneExchangeOrderService.Domain.Models.V2.Reservation.Request
{
    [ExcludeFromCodeCoverage]
    public class PriceLevel
    {
        [JsonPropertyName("status")] public bool? Status { get; set; }
        [JsonPropertyName("priceLevel")] public string? PriceLevelCode { get; set; }
        [JsonPropertyName("currency")] public string? Currency { get; set; }
        [JsonPropertyName("totalPrice")] public decimal? TotalPrice { get; set; }
        [JsonPropertyName("priceAttributes")] public List<AttributePair>? PriceAttributes { get; set; }
    }
}