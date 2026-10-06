using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace APITransformationsFunction.Model.Reservation.Search
{
    public class OnexSearchReservationRequest
    {
        public string Id { get; set; }
        public string Source { get; set; }
        public string Specversion { get; set; }
        public string Type { get; set; }
        public string CorrelationId { get; set; }
        public string Datacontenttype { get; set; }
        public string Dataschema { get; set; }
        public string Subject { get; set; }
        public DateTime? Time { get; set; }
        public bool? Ispartial { get; set; }
        public ReservationSearchData Data { get; set; }
    }

    public class ReservationSearchData
    {
        public SystemMeta SystemMeta { get; set; }
        public SearchCriteria SearchCriteria { get; set; }
    }

    public class SystemMeta
    {
        public string TenantID { get; set; }
        public DateTime? InsertedDateTime { get; set; }
        public string InsertedBy { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
        public string UpdatedBy { get; set; }
        public bool? Status { get; set; }
        public string StatusReason { get; set; }
    }

    public class SearchCriteria
    {
        public string TradingAgreementCode { get; set; }
        public List<SourceIdentifier> SourceReservationIds { get; set; }
        public List<SourceIdentifier> SourceMemberIds { get; set; }
        public string Status { get; set; }
        public DateTime? ReservationDate { get; set; }
        public int? ReservationDateInterval { get; set; }
        public DateTime? ArrivalDate { get; set; }
        public int? ArrivalDateInterval { get; set; }
        public OrderBy OrderBy { get; set; }
    }

    public class SourceIdentifier
    {
        public string SourceSystem { get; set; }
        public string Id { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class OrderBy
    {
        public string Field { get; set; }
        public string Direction { get; set; } // Expected values: "asc" or "desc"
    }
}
