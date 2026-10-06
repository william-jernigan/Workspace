using ExchangeReservationFunction.Model.Reservation.Create.Request;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ExchangeReservationFunction.Model.Reservation.Search
{
    public class OnexSearchReservationResponse
    {

        [JsonPropertyName("id")] public string Id { get; set; } = default!;
        [JsonPropertyName("source")] public string Source { get; set; } = default!;
        [JsonPropertyName("specversion")] public string Specversion { get; set; } = "1.0";
        [JsonPropertyName("type")] public string Type { get; set; } = default!;
        [JsonPropertyName("datacontenttype")] public string? Datacontenttype { get; set; }
        [JsonPropertyName("dataschema")] public string? Dataschema { get; set; }
        [JsonPropertyName("subject")] public string? Subject { get; set; }
        [JsonPropertyName("time")] public DateTimeOffset? Time { get; set; }
        [JsonPropertyName("ispartial")] public bool? Ispartial { get; set; }
        [JsonPropertyName("data")] public ReservationSearchResponseData Data { get; set; } = default!;
    }
    public class ReservationSearchResponseData
    {
        [JsonPropertyName("systemMeta")] public SystemMeta SystemMeta { get; set; } = default!;
        [JsonPropertyName("externalReference")] public ExternalReference ExternalReference { get; set; } = new();
        [JsonPropertyName("pagination")] public ResponsePagination Pagination { get; set; } = new();
        [JsonPropertyName("results")] public List<SearchBookingItemResponse> Results { get; set; } = [];
    }
    public class ResponsePagination
    {
        [JsonPropertyName("page")] public int Page { get; set; }
        [JsonPropertyName("pageSize")] public int PageSize { get; set; }
        [JsonPropertyName("totalItems")] public int TotalItems { get; set; }
        [JsonPropertyName("totalPages")] public int TotalPages { get; set; }
    }
    public class SearchBookingItemResponse
    {
        public required string ItemCode { get; set; }
        public string? ItemClass { get; set; }
        public required string ParentCode { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public List<AttributePair>? ItemAttributes { get; set; }
        public required string MemberID { get; set; }
        public DateTime? ReservationDate { get; set; }
        public required string StartDate { get; set; }
        public required string EndDate { get; set; }
        public int? WeekNumber { get; set; }
        public string? Action { get; set; }
        public GuestCounts GuestCounts { get; set; } = new();
        public bool? IsOwnerBooking { get; set; }
        public GuestInfo GuestInfo { get; set; } = new();
        public PriceLevel? PriceLevel { get; set; }
        public List<AttributePair>? BookingAttributes { get; set; }
        public List<AttributePair>? BookingComments { get; set; }
    }
}
