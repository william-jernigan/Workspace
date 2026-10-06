using Microsoft.EntityFrameworkCore;
using OneExchangeOrderService.Domain.Entities.ExchangeOrder;
using OneExchangeOrderService.Domain.Persistence.Repositories.ExchangeReservation;
using OneExchangeOrderService.Infrastructure.DbContexts;

namespace OneExchangeOrderService.Infrastructure.Persistance.Repositories.ExchangeReservation
{
    public sealed class OneExchangeOrderRepository(OneExchangeDbContext context) : IOneExchangeOrderRepository
    {
        private readonly OneExchangeDbContext _context = context;

        public async Task CreateOrderAsync(Order order) => _context.Orders.Add(order);

        public async Task CreateOrderReturnsAsync(OrderReturn orderReturn) => await context.OrderReturns.AddAsync(orderReturn);
        public async Task<Order?> GetOrderByExternalReferenceIdAsync(string externalReferenceId, CancellationToken cancellationToken = default)
        {
            var order = await context.Orders.Where(x => x.ExternalReferenceId == externalReferenceId)
                     .Include(x => x.OrderItems)
                     .ThenInclude(x => x.OrderItemAttributes)
                     .Include(x => x.OrderGuests)
                     .Include(x => x.OrderPayments)
                     .Include(x => x.OrderReturn)
                     .Include(x => x.ExchangeTransactionLogs)
                     .Include(x => x.OrderRefunds)
                     .TagWith(nameof(GetOrderByExternalReferenceIdAsync))
                     .FirstOrDefaultAsync(cancellationToken);

            return order;
        }
    }

}
