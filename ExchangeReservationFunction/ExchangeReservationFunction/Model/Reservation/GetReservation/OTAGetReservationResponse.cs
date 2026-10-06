using ExchangeReservationFunction.Model.Reservation.Create.Request;
using ExchangeReservationFunction.Model.Reservation.Common;
using System.Collections.Generic;

namespace ExchangeReservationFunction.Model.Reservation.GetReservation
{
    public class OTAGetReservationResponse
    {
        public string CorrelationID { get; set; }
        public string TradingAgreementCode { get; set; }
        public string MembershipClub { get; set; }
        public string MembershipClubLevel { get; set; }
        public List<ReservationGetDetails> Reservations { get; set; }
    }

    public class ReservationGetDetails
    {
        public string Status { get; set; }
        public string ReservationDate { get; set; }
        public string ArrivalDate { get; set; }
        public string DepartureDate { get; set; }
        public string ReservationType { get; set; }
        public string ReservationSubType { get; set; }
        public List<SourceReservationId> SourceReservationIds { get; set; }
        public CancellationInfo CancellationInfo { get; set; }
        public string Comment { get; set; }
        public string Category { get; set; }
        public List<PropertyDetails> Properties { get; set; }
        public List<ReservationComment> ReservationComments { get; set; }
    }
}
