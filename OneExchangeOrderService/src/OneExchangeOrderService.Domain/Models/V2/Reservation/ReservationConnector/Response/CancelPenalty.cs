namespace OneExchangeOrderService.Domain.Models.V2.Reservation.ReservationConnector.Response
{
    public class CancelPenalty
    {
        public bool NonRefundable { get; set; }
        public Deadline Deadline { get; set; }
        public AmountPercent AmountPercent { get; set; }
        public string PenaltyDescription { get; set; }
    }
}
