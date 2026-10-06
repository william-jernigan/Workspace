using System;
using System.Text.Json.Serialization;

namespace ExchangeReservationFunction.Model.Reservation.Create.Response
{

    public class ReservationResponse
    {
        [JsonPropertyName("id")] public string Id { get; set; } = default!;
        [JsonPropertyName("source")] public string Source { get; set; } = default!;
        [JsonPropertyName("specversion")] public string Specversion { get; set; } = "1.0";
        [JsonPropertyName("type")] public string Type { get; set; } = "Reservation-Response";
        [JsonPropertyName("datacontenttype")] public string Datacontenttype { get; set; } = "application/json";
        [JsonPropertyName("dataschema")] public string Dataschema { get; set; } = "https://schemas.onetruth.example/reservations/response/2020-12";
        [JsonPropertyName("subject")] public string Subject { get; set; }
        [JsonPropertyName("time")] public DateTimeOffset Time { get; set; }
        [JsonPropertyName("ispartial")] public bool Ispartial { get; set; }
        [JsonPropertyName("data")] public ReservationResponseData Data { get; set; } = new();
    }

}
