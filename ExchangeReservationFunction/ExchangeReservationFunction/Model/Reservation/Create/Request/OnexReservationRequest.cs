using System;
using System.Text.Json.Serialization;

namespace ExchangeReservationFunction.Model.Reservation.Create.Request
{
    // The same model is used for OnexCreateReservation Request and OnexGetReservation Response
    public class OnexReservationRequest
    {
        [JsonPropertyName("id")] public string Id { get; set; } = default!;
        [JsonPropertyName("source")] public string Source { get; set; } = default!;
        [JsonPropertyName("specversion")] public string Specversion { get; set; } = "1.0";
        [JsonPropertyName("type")] public string Type { get; set; } = default!;
        [JsonPropertyName("datacontenttype")] public string Datacontenttype { get; set; }
        [JsonPropertyName("dataschema")] public string Dataschema { get; set; }
        [JsonPropertyName("subject")] public string Subject { get; set; }
        [JsonPropertyName("time")] public DateTimeOffset? Time { get; set; }
        [JsonPropertyName("ispartial")] public bool? Ispartial { get; set; }
        [JsonPropertyName("data")] public ReservationRequestData Data { get; set; } = default!;
    }
}
