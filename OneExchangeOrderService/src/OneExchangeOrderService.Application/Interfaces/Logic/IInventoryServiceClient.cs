using OneExchangeOrderService.DTO.Models.Availability.ChoiceInventory.Request;
using OneExchangeOrderService.DTO.Models.Availability.ChoiceInventory.Response;
using OneExchangeOrderService.DTO.Models.InventoryUpdate.Request;
using OneExchangeOrderService.DTO.Models.InventoryUpdate.Response;
namespace OneExchangeOrderService.Application.Interfaces.Logic
{
    public interface IInventoryServiceClient
    {
        public Task<HotelAvailabilityResponse?> CheckAvailabilityAsync(List<HotelAvailabilityRequest> requests);
        public Task<InventoryResponse> UpdateInventoryAsync(InventoryRequest request);
    }
}