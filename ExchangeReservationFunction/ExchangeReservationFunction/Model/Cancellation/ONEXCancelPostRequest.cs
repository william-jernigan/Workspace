using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace ExchangeReservationFunction.Model.Cancellation
{
    public class ONEXCancelPostRequest
    {
        public string? Id { get; set; }
        public string? Source { get; set; }
        public string? Specversion { get; set; }
        public string? Type { get; set; }
        public string? Datacontenttype { get; set; }
        public string? Dataschema { get; set; }
        public string? Subject { get; set; }
        public DateTimeOffset? Time { get; set; }
        public bool? Ispartial { get; set; }
        public required CancelItemRequest Data { get; set; }
    }

    public class CancelItemRequest
    {
        public SystemMeta? SystemMeta { get; set; }
        public ExternalReference? ExternalReference { get; set; }
        public string? BookingId { get; set; }
        public string? CancellationReason { get; set; }
        public DateTimeOffset CancellationDate { get; set; }
        public decimal? RefundedAmount { get; set; }
        public string? CancellationStatus { get; set; }
        public List<BookingItem>? BookingItems { get; set; }
        public ErrorInfo? Error { get; set; }
    }
    public class SystemMeta
    {
        public string TenantID { get; set; } = default!;

        [JsonPropertyName("_insertedDateTime")]
        public DateTimeOffset InsertedDateTime { get; set; }

        [JsonPropertyName("_insertedBy")]
        public string? InsertedBy { get; set; }

        [JsonPropertyName("_updatedDateTime")]
        public DateTimeOffset UpdatedDateTime { get; set; }

        [JsonPropertyName("_updatedBy")]
        public string? UpdatedBy { get; set; }
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

    [ExcludeFromCodeCoverage]
    public class ErrorInfo
    {
        public string? ErrorCode { get; set; }
        public string? ErrorType { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class BookingItem
    {
        public string? ItemCode { get; set; }
        public string? ItemClass { get; set; }
        public string? ParentCode { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public List<AttributePair>? ItemAttributes { get; set; }
        public string? MemberID { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public int? WeekNumber { get; set; }
        public string? Action { get; set; }
        public GuestCounts? GuestCounts { get; set; } = new();
        public bool? IsOwnerBooking { get; set; }
        public GuestInfo? GuestInfo { get; set; } = new();
        public PriceLevel? PriceLevel { get; set; }
        public List<AttributePair>? BookingAttributes { get; set; }
        public List<AttributePair>? BookingComments { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class AttributePair
    {
        public string? Attribute { get; set; }
        public string? Value { get; set; }
    }
    public class GuestCounts
    {
        public int? Adults { get; set; }
        public int? Children { get; set; }
    }
    public class GuestInfo
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public List<PostalAddress>? PostalAddresses { get; set; }
        public List<PhoneNumber>? PhoneNumbers { get; set; }
        public List<DigitalAddress>? DigitalAddresses { get; set; }
    }
    public class DigitalAddress
    {
        [JsonPropertyName("_sequence")]
        public int? Sequence { get; set; }
        public string? DigitalAddressType { get; set; }

        [JsonPropertyName("digitalAddress")]
        public string? DigitalAddressValue { get; set; }
    }
    public class PriceLevel
    {
        public bool? Status { get; set; }

        [JsonPropertyName("priceLevel")]
        public string? PriceLevelCode { get; set; }
        public string? Currency { get; set; }
        public decimal? TotalPrice { get; set; }
        public List<AttributePair>? PriceAttributes { get; set; }
    }
    public class PostalAddress
    {
        [JsonPropertyName("_sequence")]
        public int? Sequence { get; set; }
        public string? AddressType { get; set; }
        public string Line1 { get; set; } = default!;
        public string? Line2 { get; set; }
        public string? City { get; set; } = default!;
        public string? State { get; set; } = default!;
        public string? PostalCode { get; set; } = default!;
        public string? Country { get; set; } = default!;
    }
    public class PhoneNumber
    {
        [JsonPropertyName("_sequence")]
        public int? Sequence { get; set; }
        public string? PhoneType { get; set; }
        public string? Number { get; set; }
    }
}
