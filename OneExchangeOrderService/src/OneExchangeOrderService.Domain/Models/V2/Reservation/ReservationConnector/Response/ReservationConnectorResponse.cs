namespace OneExchangeOrderService.Domain.Models.V2.Reservation.ReservationConnector.Response
{
    public class ReservationConnectorResponse
    {
        public string Id { get; set; }
        public string Source { get; set; }
        public string SpecVersion { get; set; }
        public string Type { get; set; }
        public ReservationConnectorResponseData Data { get; set; }
    }
}
