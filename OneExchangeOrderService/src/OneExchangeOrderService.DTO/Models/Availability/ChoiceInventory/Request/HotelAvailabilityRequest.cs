namespace OneExchangeOrderService.DTO.Models.Availability.ChoiceInventory.Request
{
    public class HotelAvailabilityRequest
    {
        public string? HotelCode { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public Occupancy Occupancy { get; set; }
    }
}
