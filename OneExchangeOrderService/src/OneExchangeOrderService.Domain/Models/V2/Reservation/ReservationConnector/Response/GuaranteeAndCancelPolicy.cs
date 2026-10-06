namespace OneExchangeOrderService.Domain.Models.V2.Reservation.ReservationConnector.Response
{
    public class GuaranteeAndCancelPolicy
    {
        public string GuaranteeDescription { get; set; }
        public string GuaranteeType { get; set; }
        public List<CancelPenalty> CancelPenalties { get; set; }
    }
}
