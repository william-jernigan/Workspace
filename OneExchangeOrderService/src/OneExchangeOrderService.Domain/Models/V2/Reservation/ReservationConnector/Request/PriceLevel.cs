using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace OneExchangeOrderService.Domain.Models.V2.Reservation.ReservationConnector.Request
{
    [ExcludeFromCodeCoverage]
    public class PriceLevel
    {
        public bool Status { get; set; }
        [JsonPropertyName("priceLevel")]
        public string PriceLevelName { get; set; }
        public string Currency { get; set; }
        public decimal? TotalPrice { get; set; }
        public List<AttributePair>? PriceAttributes { get; set; }
    }
}
