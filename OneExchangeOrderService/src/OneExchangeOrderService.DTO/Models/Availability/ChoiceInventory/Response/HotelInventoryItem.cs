namespace OneExchangeOrderService.DTO.Models.Availability.ChoiceInventory.Response
{
    public class HotelInventoryItem
    {
        public string ItemCode { get; set; }
        public string ItemClass { get; set; }
        public string ParentCode { get; set; }
        public string Name { get; set; }
        public List<ItemAttributeResponse> ItemAttributes { get; set; } = [];
        public ItemAvailability ItemAvailability { get; set; }
    }
}
