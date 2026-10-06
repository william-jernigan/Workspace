using OneExchangeOrderService.Domain.Entities.ExchangeOrder;
using OneExchangeOrderService.Domain.Persistence.Repositories.ExchangeOrder;
using OneExchangeOrderService.Infrastructure.DbContexts;

namespace OneExchangeOrderService.Infrastructure.Persistance.Repositories.ExchangeOrder
{
    public class OrderRefundRepository(ExchangeOrderDbContext context) : IOrderRefundRepository
    {
        public async Task CreateOrderRefundAsync(List<OrderRefund> orderRefunds) => await context.OrderRefunds.AddRangeAsync(orderRefunds);
    }
}
