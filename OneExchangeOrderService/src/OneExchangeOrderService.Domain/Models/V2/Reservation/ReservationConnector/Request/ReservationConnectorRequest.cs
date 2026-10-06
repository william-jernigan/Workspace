namespace OneExchangeOrderService.Domain.Models.V2.Reservation.ReservationConnector.Request
{
    public class ReservationConnectorRequest
    {
        public string Id { get; set; }
        public string Source { get; set; }
        public string Specversion { get; set; }
        public string Type { get; set; }
        public ReservationConnectorRequestData Data { get; set; }
    }
}
