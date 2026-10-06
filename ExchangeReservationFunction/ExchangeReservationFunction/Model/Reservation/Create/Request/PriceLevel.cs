using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ExchangeReservationFunction.Model.Reservation.Create.Request
{
    public class PriceLevel
    {
        [JsonPropertyName("status")] public bool? Status { get; set; }
        [JsonPropertyName("priceLevel")] public string PriceLevelCode { get; set; }
        [JsonPropertyName("currency")] public string Currency { get; set; }
        [JsonPropertyName("totalPrice")] public decimal? TotalPrice { get; set; }
        [JsonPropertyName("priceAttributes")] public List<AttributePair> PriceAttributes { get; set; }
    }
}
