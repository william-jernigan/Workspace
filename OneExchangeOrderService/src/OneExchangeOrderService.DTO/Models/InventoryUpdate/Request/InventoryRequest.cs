namespace OneExchangeOrderService.DTO.Models.InventoryUpdate.Request
{
    public class InventoryRequest
    {
        public required string UnitCode { get; set; }
        public required DateTime CheckInDate { get; set; }
        public required DateTime CheckOutDate { get; set; }
        public required int Quantity { get; set; }
        public string? Reason { get; set; }
    }
}
