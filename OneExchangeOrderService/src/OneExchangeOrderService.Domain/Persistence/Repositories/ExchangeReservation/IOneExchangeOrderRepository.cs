using OneExchangeOrderService.Domain.Entities.ExchangeOrder;

namespace OneExchangeOrderService.Domain.Persistence.Repositories.ExchangeReservation
{
    public interface IOneExchangeOrderRepository
    {
        Task CreateOrderAsync(Order order);
        public Task CreateOrderReturnsAsync(OrderReturn orderReturn);
        Task<Order?> GetOrderByExternalReferenceIdAsync(string externalReferenceId, CancellationToken cancellationToken = default);
    }
}
