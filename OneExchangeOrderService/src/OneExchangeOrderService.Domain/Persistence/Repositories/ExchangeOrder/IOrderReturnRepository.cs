using OneExchangeOrderService.Domain.Entities.ExchangeOrder;

namespace OneExchangeOrderService.Domain.Persistence.Repositories.ExchangeOrder
{
    public interface IOrderReturnRepository
    {
        public Task CreateOrderReturnsAsync(OrderReturn orderReturn);

        Task<string?> GetCancellationNumberAsync(string orderId);
    }
}
