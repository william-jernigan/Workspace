using OneExchangeOrderService.Domain.Persistence.Repositories.ExchangeOrder;
using OneExchangeOrderService.Domain.Persistence.Repositories.ExchangeProfile;
using OneExchangeOrderService.Infrastructure.Persistance.Repositories.ExchangeOrder;

namespace OneExchangeOrderService.Application.Interfaces
{
    public interface IOrderUnitOfWork
    {
        IOrderRepository OrderRepository { get; }
        IProfileRepository ProfileRepository { get; }
        IOrderPaymentRepository OrderPaymentRepository { get; }
        IExchangeTransactionLogRepository ExchangeTransactionLogRepository { get; }
        IOrderReturnRepository OrderReturnRepository { get; }
        IAddressRepository AddressRepository { get; }
        IOrderRefundRepository OrderRefundRepository { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        IOrderItemAttributeRepository OrderItemAttributeRepository { get; }
    }

}
