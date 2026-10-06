using System.Text.Json.Serialization;

namespace OneExchangeOrderService.Domain.Models.V2.Reservation.Response
{
    public class CancelDeadline
    {
        [JsonPropertyName("absoluteDeadline")] public DateTimeOffset? AbsoluteDeadline { get; set; }
    }
}