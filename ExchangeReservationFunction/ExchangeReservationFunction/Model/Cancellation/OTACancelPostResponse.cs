using System.Collections.Generic;

namespace ExchangeReservationFunction.Model.Cancellation
{
    public class OTACancelPostResponse
    {
        public string CorrelationID { get; set; }
        public string TradingAgreementCode { get; set; }
        public ResponseReservationDetails Reservations { get; set; }
        public ResponseCommonCancellationInfo CancellationInfo { get; set; }
        public string Message { get; set; }
        public List<FailureError> Errors { get; set; }
    }

    public class ResponseReservationDetails
    {
        public List<ResponseSourceReservationId> SourceReservationIds { get; set; }
    }

    public class ResponseSourceReservationId
    {
        public string SourceSystem { get; set; }
        public string Id { get; set; }
    }
    public class FailureError
    {
        public string Type { get; set; }
        public string Code { get; set; }
        public int Status { get; set; }
        public string ShortText { get; set; }
    }
}
