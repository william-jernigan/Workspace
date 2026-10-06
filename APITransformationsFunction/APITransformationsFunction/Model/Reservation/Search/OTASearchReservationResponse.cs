using APITransformationsFunction.Model.Reservation.Create.Request;
using APITransformationsFunction.Model.Reservation.Shared;
using System.Collections.Generic;

namespace APITransformationsFunction.Model.Reservation.Search
{
    public class OTASearchReservationResponse
    {
        public string CorrelationID { get; set; }
        public string TradingAgreementCode { get; set; }
        public string MembershipClub { get; set; }
        public string MembershipClubLevel { get; set; }
        public Pagination Pagination { get; set; }
        public List<ReservationDetails> Reservations { get; set; }
    }

    public class Pagination
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
    }

    public class ReservationDetails
    {
        public string Status { get; set; }
        public string ReservationDate { get; set; }
        public string ArrivalDate { get; set; }
        public string DepartureDate { get; set; }
        public List<SourceReservationId> SourceReservationIds { get; set; }
        public CancellationInfo CancellationInfo { get; set; }
        public string Comment { get; set; }
        public string Category { get; set; }
        public List<PropertyDetails> Properties { get; set; }
        public List<ReservationComment> ReservationComments { get; set; }
    }
}
