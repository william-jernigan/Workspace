using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace APITransformationsFunction.Model.Reservation.Create.Request
{
    public class ReservationRequestData
    {
        [JsonPropertyName("systemMeta")] public SystemMeta SystemMeta { get; set; } = default!;
        [JsonPropertyName("externalReference")] public ExternalReference ExternalReference { get; set; } = new();
        [JsonPropertyName("bookingItems")] public List<BookingItemRequest> BookingItems { get; set; } = [];
    }
}
