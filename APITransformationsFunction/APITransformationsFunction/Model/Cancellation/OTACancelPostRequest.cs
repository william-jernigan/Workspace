using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace APITransformationsFunction.Model.Cancellation
{
    public class OTACancelPostRequest
    {
        [Required(ErrorMessage = "CorrelationID is required.")]
        public string CorrelationID { get; set; }
        [Required(ErrorMessage = "TradingAgreementCode is required.")]
        public string TradingAgreementCode { get; set; }
        [Required(ErrorMessage = "Reservations are required.")]
        public ReservationDetails Reservations { get; set; }
        [Required(ErrorMessage = "CancellationInfo is required.")]
        public CommonCancellationInfo CancellationInfo { get; set; }
    }

    public class ReservationDetails
    {
        [Required(ErrorMessage = "SourceReservationIds are required.")]
        public List<SourceReservationId> SourceReservationIds { get; set; }
    }

    public class SourceReservationId
    {
        public string SourceSystem { get; set; }
        public string Id { get; set; }
    }

    public class CommonCancellationInfo
    {
        public string CancelReason { get; set; }
        public DateTime CancelDate { get; set; }
        public string CancelComments { get; set; }
    }
}
