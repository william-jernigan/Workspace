using OneExchangeOrderService.Domain.Entities.ExchangeOrder;
using OneExchangeOrderService.Infrastructure.DbContexts;

namespace OneExchangeOrderService.Infrastructure.Persistance.Repositories.ExchangeOrder
{
    public class OrderItemAttributeRepository(ExchangeOrderDbContext context) : IOrderItemAttributeRepository
    {
        public async Task AddRangeAsync(List<OrderItemAttribute> attributes) => await context.OrderItemAttributes.AddRangeAsync(attributes);
    }
}
