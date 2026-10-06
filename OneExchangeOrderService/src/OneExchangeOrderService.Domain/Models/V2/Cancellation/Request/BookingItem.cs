using OneExchangeOrderService.Domain.Models.V2.Reservation.Request;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace OneExchangeOrderService.Domain.Models.V2.Cancellation.Request
{
    [ExcludeFromCodeCoverage]
    public class BookingItem
    {
        [JsonPropertyName("itemCode")] public string? ItemCode { get; set; } = default!;
        [JsonPropertyName("itemClass")] public string? ItemClass { get; set; }
        [JsonPropertyName("parentCode")] public string? ParentCode { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }

        [JsonPropertyName("itemAttributes")] public List<AttributePair>? ItemAttributes { get; set; }

        [JsonPropertyName("memberID")] public string? MemberID { get; set; }
        [JsonPropertyName("startDate")] public DateTime StartDate { get; set; }
        [JsonPropertyName("endDate")] public DateTime EndDate { get; set; }
        [JsonPropertyName("weekNumber")] public int? WeekNumber { get; set; }
        [JsonPropertyName("action")] public string? Action { get; set; }

        [JsonPropertyName("guestCounts")] public GuestCounts? GuestCounts { get; set; } = new();
        [JsonPropertyName("isOwnerBooking")] public bool? IsOwnerBooking { get; set; }

        [JsonPropertyName("guestInfo")] public GuestInfo? GuestInfo { get; set; } = new();

        [JsonPropertyName("priceLevel")] public PriceLevel? PriceLevel { get; set; }

        [JsonPropertyName("bookingAttributes")] public List<AttributePair>? BookingAttributes { get; set; }
        [JsonPropertyName("bookingComments")] public List<AttributePair>? BookingComments { get; set; }
    }
}
