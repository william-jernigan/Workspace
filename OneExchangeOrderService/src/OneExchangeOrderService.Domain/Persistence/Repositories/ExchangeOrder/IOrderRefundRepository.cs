using OneExchangeOrderService.Domain.Entities.ExchangeOrder;

namespace OneExchangeOrderService.Domain.Persistence.Repositories.ExchangeOrder
{
    public interface IOrderRefundRepository
    {
        public Task CreateOrderRefundAsync(List<OrderRefund> orderRefunds);
    }
}
