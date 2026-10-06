using ExchangeReservationFunction.Model.Reservation.Common;
using System.Collections.Generic;

namespace ExchangeReservationFunction.Model.Reservation.Create.Request
{
    public class OTACreateReservationResponse
    {
        public string CorrelationID { get; set; }
        public string TradingAgreementCode { get; set; }
        public List<ReservationPostDetails> Reservations { get; set; }
    }

    public class ReservationPostDetails
    {
        public string PropertyCode { get; set; }
        public string UnitTypeCode { get; set; }
        public string Status { get; set; }
        public string CreateDateTime { get; set; }
        public string Message { get; set; }
        public List<SourceReservationId> SourceReservationIds { get; set; }
        public Policies Policies { get; set; }
        public List<ErrorDetails> Errors { get; set; }
    }
}
