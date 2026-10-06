using System.Text.Json.Serialization;

namespace APITransformationsFunction.Model.Reservation.Create.Request
{
    public class PhoneNumber
    {
        [JsonPropertyName("_sequence")] public int? Sequence { get; set; }
        [JsonPropertyName("phoneType")] public string PhoneType { get; set; } = default!; // mobile|home|work
        [JsonPropertyName("number")] public string Number { get; set; } = default!;
    }
}
