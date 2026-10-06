using ExchangeReservationFunction.Model.Reservation.Create.Request;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ExchangeReservationFunction.Model.Reservation.Search
{
    public class OTASearchReservationRequest
    {
        public string CorrelationID { get; set; }
        public string TradingAgreementCode { get; set; }
        public List<SourceReservationId> SourceReservationIds { get; set; }
        public List<SourceMemberId> SourceMemberIds { get; set; }
        public string Status { get; set; }
        public string ReservationDate { get; set; }

        public int? ReservationDateInterval { get; set; }

        public string ArrivalDate { get; set; }

        public int? ArrivalDateInterval { get; set; }

        public OrderByCriteria OrderBy { get; set; }
        public class SourceMemberId
        {
            public string SourceSystem { get; set; }
            public string Id { get; set; }
        }

        [ExcludeFromCodeCoverage]
        public class OrderByCriteria
        {
            public string Field { get; set; }
            public string Direction { get; set; } // "asc" or "desc"
        }
    }
}
