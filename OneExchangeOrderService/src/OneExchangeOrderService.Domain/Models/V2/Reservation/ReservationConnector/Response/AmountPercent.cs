namespace OneExchangeOrderService.Domain.Models.V2.Reservation.ReservationConnector.Response
{
    public class AmountPercent
    {
        public int Amount { get; set; }
        public bool FeesInclusive { get; set; }
        public bool TaxInclusive { get; set; }
    }
}
