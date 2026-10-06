using System.Text.Json.Serialization;

namespace ExchangeReservationFunction.Model.Reservation.Create.Request
{
    public class AttributePair
    {
        [JsonPropertyName("attribute")] public string Attribute { get; set; } = default!;
        [JsonPropertyName("value")] public string Value { get; set; } = default!;
    }
}
