using OneExchangeOrderService.Application.Models.Resort;

namespace OneExchangeOrderService.Application.Interfaces.Logic
{
    public interface IContentfulService
    {
        Task<ResortPage> GetResortDetailsAsync(string resortCode);

    }
}
