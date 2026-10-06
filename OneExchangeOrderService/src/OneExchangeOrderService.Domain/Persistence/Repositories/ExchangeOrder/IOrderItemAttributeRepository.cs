using OneExchangeOrderService.Domain.Entities.ExchangeOrder;

namespace OneExchangeOrderService.Infrastructure.Persistance.Repositories.ExchangeOrder
{
    public interface IOrderItemAttributeRepository
    {
        public Task AddRangeAsync(List<OrderItemAttribute> attributes);
    }
}
