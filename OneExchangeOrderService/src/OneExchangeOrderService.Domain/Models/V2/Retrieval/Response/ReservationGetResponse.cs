using System.Text.Json.Serialization;

namespace OneExchangeOrderService.Domain.Models.V2.Retrieval.Response
{
    public class ReservationGetResponse
    {
        public string Id { get; set; } = default!;
        public string Source { get; set; } = default!;
        public string Specversion { get; set; } = "1.0";
        public string Type { get; set; } = default!;
        public string? Datacontenttype { get; set; }
        public string? Dataschema { get; set; }
        public string? Subject { get; set; }
        public DateTimeOffset? Time { get; set; }
        public bool? Ispartial { get; set; }
        public ReservationRetrievalData Data { get; set; } = default!;
    }

    public class ReservationRetrievalData
    {
        public SystemMeta SystemMeta { get; set; } = default!;
        public ExternalReference ExternalReference { get; set; } = new();
        public List<RetrievalBookingItem> BookingItems { get; set; } = [];
    }

    public class SystemMeta
    {
        public string TenantID { get; set; } = default!;
        [JsonPropertyName("_insertedDateTime")] public DateTimeOffset? InsertedDateTime { get; set; }
        [JsonPropertyName("_insertedBy")] public string? InsertedBy { get; set; }
        [JsonPropertyName("_updatedDateTime")] public DateTimeOffset? UpdatedDateTime { get; set; }
        [JsonPropertyName("_updatedBy")] public string? UpdatedBy { get; set; }
        public bool? Status { get; set; }
        public string? StatusReason { get; set; }
    }

    public class ExternalReference
    {
        public string? SourceSystem { get; set; }
        public string? TargetSystem { get; set; }
        public string? SourceReferenceId { get; set; }
        public string? TargetReferenceId { get; set; }
    }
    public class RetrievalBookingItem
    {
        public required string ItemCode { get; set; }
        public string? ItemClass { get; set; }
        public required string ParentCode { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public List<AttributePair>? ItemAttributes { get; set; }
        public required string MemberID { get; set; }
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

    public class GuestCounts
    {
        public int? Adults { get; set; }
        public int? Children { get; set; }
    }

    public class GuestInfo
    {
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public List<PostalAddress>? PostalAddresses { get; set; }
        public List<PhoneNumber>? PhoneNumbers { get; set; }
        public List<DigitalAddress>? DigitalAddresses { get; set; }
    }
    public class PriceLevel
    {
        public bool? Status { get; set; }
        [JsonPropertyName("priceLevel")] public string? PriceLevelCode { get; set; }
        public string? Currency { get; set; }
        public decimal? TotalPrice { get; set; }
        public List<AttributePair>? PriceAttributes { get; set; }
    }
    public class AttributePair
    {
        public string Attribute { get; set; } = default!;
        public string Value { get; set; } = default!;
    }
    public class PostalAddress
    {
        [JsonPropertyName("_sequence")] public int? Sequence { get; set; }
        public string? AddressType { get; set; }
        public string Line1 { get; set; } = default!;
        public string? Line2 { get; set; }
        public string City { get; set; } = default!;
        public string State { get; set; } = default!;
        public string PostalCode { get; set; } = default!;
        public string Country { get; set; } = default!;
    }
    public class PhoneNumber
    {
        [JsonPropertyName("_sequence")] public int? Sequence { get; set; }
        public string PhoneType { get; set; } = default!; // mobile|home|work
        public string Number { get; set; } = default!;
    }
    public class DigitalAddress
    {
        [JsonPropertyName("_sequence")] public int? Sequence { get; set; }
        public string DigitalAddressType { get; set; } = default!; // email
        [JsonPropertyName("digitalAddress")] public string DigitalAddressValue { get; set; } = default!;
    }
}
