using OneExchangeOrderService.Domain.Entities.ExchangeOrder;
using OneExchangeOrderService.Infrastructure.DbContexts;

namespace OneExchangeOrderService.Infrastructure.Persistance.Repositories.ExchangeOrder
{
    public class OrderPaymentRepository(ExchangeOrderDbContext context) : IOrderPaymentRepository
    {
        public async Task CreateOrderPaymentsAsync(List<OrderPayment> payments) => await context.OrderPayments.AddRangeAsync(payments);
    }
}
