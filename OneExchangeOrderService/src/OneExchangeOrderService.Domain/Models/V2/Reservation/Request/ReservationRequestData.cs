using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace OneExchangeOrderService.Domain.Models.V2.Reservation.Request
{
    [ExcludeFromCodeCoverage]
    public class ReservationRequestData
    {
        [JsonPropertyName("systemMeta")] public required SystemMeta SystemMeta { get; set; } = default!;
        [JsonPropertyName("externalReference")] public required ExternalReference ExternalReference { get; set; } = new();
        [JsonPropertyName("bookingItems")] public required List<BookingItemRequest> BookingItems { get; set; } = [];
    }
}