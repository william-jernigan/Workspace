using OneExchangeOrderService.Domain.Persistence.Repositories.ExchangeOrder;
using OneExchangeOrderService.Domain.Persistence.Repositories.ExchangeProfile;

namespace OneExchangeOrderService.Infrastructure.Persistance.Repositories.ExchangeOrder
{
    public class OrderRepositories
    {
        public required IOrderRepository OrderRepository { get; init; }
        public required IProfileRepository ProfileRepository { get; init; }
        public required IOrderPaymentRepository OrderPaymentRepository { get; init; }
        public required IExchangeTransactionLogRepository ExchangeTransactionLogRepository { get; init; }
        public required IOrderReturnRepository OrderReturnRepository { get; init; }
        public required IAddressRepository AddressRepository { get; init; }
        public required IOrderRefundRepository OrderRefundRepository { get; init; }
        public required IOrderItemAttributeRepository OrderItemAttributeRepository { get; init; }
    }
}
