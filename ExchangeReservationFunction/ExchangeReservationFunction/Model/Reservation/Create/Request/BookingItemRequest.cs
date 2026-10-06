using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ExchangeReservationFunction.Model.Reservation.Create.Request
{
    public class BookingItemRequest
    {
        [JsonPropertyName("itemCode")] public string ItemCode { get; set; } = default!;
        [JsonPropertyName("itemClass")] public string ItemClass { get; set; }
        [JsonPropertyName("parentCode")] public string ParentCode { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }

        [JsonPropertyName("itemAttributes")] public List<AttributePair> ItemAttributes { get; set; }

        [JsonPropertyName("memberID")] public string MemberID { get; set; }
        [JsonPropertyName("reservationDate")] public DateTime? ReservationDate { get; set; }
        [JsonPropertyName("startDate")] public DateTimeOffset StartDate { get; set; }
        [JsonPropertyName("endDate")] public DateTimeOffset EndDate { get; set; }
        [JsonPropertyName("weekNumber")] public int? WeekNumber { get; set; }
        [JsonPropertyName("action")] public string Action { get; set; }

        [JsonPropertyName("guestCounts")] public GuestCounts GuestCounts { get; set; } = new();
        [JsonPropertyName("isOwnerBooking")] public bool? IsOwnerBooking { get; set; }

        [JsonPropertyName("guestInfo")] public GuestInfo GuestInfo { get; set; } = new();

        [JsonPropertyName("priceLevel")] public PriceLevel PriceLevel { get; set; }

        [JsonPropertyName("bookingAttributes")] public List<AttributePair> BookingAttributes { get; set; }
        [JsonPropertyName("bookingComments")] public List<AttributePair> BookingComments { get; set; }
    }
}
