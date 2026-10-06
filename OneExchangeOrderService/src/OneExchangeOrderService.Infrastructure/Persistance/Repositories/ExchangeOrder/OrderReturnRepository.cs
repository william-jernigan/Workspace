using Microsoft.EntityFrameworkCore;
using OneExchangeOrderService.Domain.Entities.ExchangeOrder;
using OneExchangeOrderService.Domain.Persistence.Repositories.ExchangeOrder;
using OneExchangeOrderService.Infrastructure.DbContexts;

namespace OneExchangeOrderService.Infrastructure.Persistance.Repositories.ExchangeOrder
{
    public class OrderReturnRepository(ExchangeOrderDbContext context) : IOrderReturnRepository
    {
        public async Task CreateOrderReturnsAsync(OrderReturn orderReturn) => await context.OrderReturns.AddAsync(orderReturn);
        public async Task<string?> GetCancellationNumberAsync(string orderId)
        {
            if (!Guid.TryParse(orderId, out var parsedOrderId))
            {
                return null;
            }

            return await context.OrderReturns?.AsNoTracking().Where(o => o.OrderId == parsedOrderId)?.Select(o => o.ExternalReferenceId)?.FirstOrDefaultAsync();

        }



    }
}
