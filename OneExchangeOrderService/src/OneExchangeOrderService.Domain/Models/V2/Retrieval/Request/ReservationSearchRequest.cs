using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace OneExchangeOrderService.Domain.Models.V2.Retrieval.Request
{
    [ExcludeFromCodeCoverage]
    public class ReservationSearchRequest
    {
        public string Id { get; set; } = default!;
        public string Source { get; set; } = default!;
        public string Specversion { get; set; } = "1.0";
        public string Type { get; set; } = default!;
        public string? CorrelationId { get; set; }
        public string? Datacontenttype { get; set; }
        public string? Dataschema { get; set; }
        public string? Subject { get; set; }
        public DateTimeOffset? Time { get; set; }
        public bool? IsPartial { get; set; }
        public ReservationSearchRequestData Data { get; set; } = default!;
    }

    public class ReservationSearchRequestData
    {
        public RetrievalSystemMeta SystemMeta { get; set; } = default!;
        public SearchCriteria SearchCriteria { get; set; } = default!;
    }

    public class RetrievalSystemMeta
    {
        public string TenantID { get; set; } = default!;
        [JsonPropertyName("_insertedDateTime")] public DateTimeOffset? InsertedDateTime { get; set; }
        [JsonPropertyName("_insertedBy")] public string? InsertedBy { get; set; }
        [JsonPropertyName("_updatedDateTime")] public DateTimeOffset? UpdatedDateTime { get; set; }
        [JsonPropertyName("_updatedBy")] public string? UpdatedBy { get; set; }
        public bool? Status { get; set; }
        public string? StatusReason { get; set; }
    }

    public class SearchCriteria
    {
        public string TradingAgreementCode { get; set; } = default!;
        public List<SourceIdentifier>? SourceReservationIds { get; set; }
        public List<SourceIdentifier>? SourceMemberIds { get; set; }
        public string? Status { get; set; }
        public DateTime? ReservationDate { get; set; }
        public int? ReservationDateInterval { get; set; }
        public DateTime? ArrivalDate { get; set; }
        public int? ArrivalDateInterval { get; set; }
        public OrderBy? OrderBy { get; set; }
    }

    public class SourceIdentifier
    {
        public string SourceSystem { get; set; } = default!;
        public string Id { get; set; } = default!;
    }

    [ExcludeFromCodeCoverage]
    public class OrderBy
    {
        public string? Field { get; set; }
        public string? Direction { get; set; }  // "asc" or "desc"
    }
}

