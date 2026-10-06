namespace OneExchangeOrderService.DTO.Models.Availability.ChoiceInventory.Response
{
    public class HotelAvailabilityResponse
    {
        public string Id { get; set; }
        public string Source { get; set; }
        public string Specversion { get; set; }
        public string Type { get; set; }
        public string CorrelationId { get; set; }
        public string Datacontenttype { get; set; }
        public string Time { get; set; }

        public HotelAvailabilityData Data { get; set; }
    }
}