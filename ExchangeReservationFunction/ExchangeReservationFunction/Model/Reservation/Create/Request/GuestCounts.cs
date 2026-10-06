using System.Text.Json.Serialization;

namespace ExchangeReservationFunction.Model.Reservation.Create.Request
{
    public class GuestCounts
    {
        [JsonPropertyName("adults")] public int? Adults { get; set; }
        [JsonPropertyName("children")] public int? Children { get; set; }
    }
}
