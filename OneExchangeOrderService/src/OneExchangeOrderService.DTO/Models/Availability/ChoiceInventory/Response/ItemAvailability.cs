namespace OneExchangeOrderService.DTO.Models.Availability.ChoiceInventory.Response
{
    public class ItemAvailability
    {
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string Status { get; set; }
        public string UnitOfMeasure { get; set; }
        public int QuantityOnHand { get; set; }
        public List<ItemSchedulePriceLevel> ItemSchedulePriceLevels { get; set; }
    }
}
