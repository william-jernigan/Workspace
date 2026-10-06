using OneExchangeOrderService.Domain.Entities.ExchangeOrder;

namespace OneExchangeOrderService.Infrastructure.Persistance.Repositories.ExchangeOrder
{
    public interface IOrderPaymentRepository
    {
        public Task CreateOrderPaymentsAsync(List<OrderPayment> payments);
    }
}
