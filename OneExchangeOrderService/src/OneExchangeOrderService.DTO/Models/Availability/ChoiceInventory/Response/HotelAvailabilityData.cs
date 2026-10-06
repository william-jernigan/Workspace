namespace OneExchangeOrderService.DTO.Models.Availability.ChoiceInventory.Response
{
    public class HotelAvailabilityData
    {
        public SystemMeta SystemMeta { get; set; }
        public ExternalReference ExternalReference { get; set; }
        public List<HotelInventoryItem> InventoryItems { get; set; }
    }
}
