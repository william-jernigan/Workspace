using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Models.V2.Retrieval.Response
{
    [ExcludeFromCodeCoverage]
    public class ReservationSearchResponse
    {
        public string Id { get; set; } = default!;
        public string Source { get; set; } = default!;
        public string Specversion { get; set; } = "1.0";
        public string Type { get; set; } = default!;
        public string? Datacontenttype { get; set; }
        public string? Dataschema { get; set; }
        public string? Subject { get; set; }
        public DateTimeOffset? Time { get; set; }
        public bool? IsPartial { get; set; }
        public ReservationSearchResponseData Data { get; set; } = new();
    }
    public class ReservationSearchResponseData
    {
        public SystemMeta? SystemMeta { get; set; }
        public ExternalReference ExternalReference { get; set; } = new();
        public Pagination Pagination { get; set; } = new();
        public List<RetrievalBookingItem> Results { get; set; } = [];
    }
    public class Pagination
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
    }
}
