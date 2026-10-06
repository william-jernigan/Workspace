using System;

namespace ExchangeReservationFunction.Model.Cancellation
{
    public class ResponseCommonCancellationInfo
    {
        public string? CancelNumber { get; set; }
        public DateTime CancelDate { get; set; }
        public string? CancelReason { get; set; }
        public string? Status { get; set; }
    }
}
