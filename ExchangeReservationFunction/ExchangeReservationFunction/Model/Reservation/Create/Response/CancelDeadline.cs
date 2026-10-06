using System;
using System.Text.Json.Serialization;

namespace ExchangeReservationFunction.Model.Reservation.Create.Response
{
    public class CancelDeadline
    {
        [JsonPropertyName("absoluteDeadline")] public DateTimeOffset? AbsoluteDeadline { get; set; }
    }
}
