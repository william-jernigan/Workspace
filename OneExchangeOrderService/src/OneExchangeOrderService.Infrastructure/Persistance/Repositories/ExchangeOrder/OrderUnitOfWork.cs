using Microsoft.EntityFrameworkCore.Storage;
using OneExchangeOrderService.Application.Interfaces;
using OneExchangeOrderService.Domain.Persistence.Repositories.ExchangeOrder;
using OneExchangeOrderService.Domain.Persistence.Repositories.ExchangeProfile;
using OneExchangeOrderService.Infrastructure.DbContexts;

namespace OneExchangeOrderService.Infrastructure.Persistance.Repositories.ExchangeOrder
{
    public class OrderUnitOfWork(
        ExchangeOrderDbContext orderContext,
        ExchangeProfileDbContext profileContext,
        OrderRepositories repositories
            ) : IOrderUnitOfWork
    {
        private readonly ExchangeOrderDbContext _orderContext = orderContext;
        private readonly ExchangeProfileDbContext _profileContext = profileContext;

        public IOrderRepository OrderRepository { get; } = repositories.OrderRepository;
        public IProfileRepository ProfileRepository { get; } = repositories.ProfileRepository;
        public IOrderPaymentRepository OrderPaymentRepository { get; } = repositories.OrderPaymentRepository;
        public IExchangeTransactionLogRepository ExchangeTransactionLogRepository { get; } = repositories.ExchangeTransactionLogRepository;
        public IAddressRepository AddressRepository { get; } = repositories.AddressRepository;
        public IOrderReturnRepository OrderReturnRepository { get; } = repositories.OrderReturnRepository;
        public IOrderRefundRepository OrderRefundRepository { get; } = repositories.OrderRefundRepository;
        private IDbContextTransaction? _orderTransaction;
        private IDbContextTransaction? _profileTransaction;
        public IOrderItemAttributeRepository OrderItemAttributeRepository { get; } = repositories.OrderItemAttributeRepository;

        public async Task BeginTransactionAsync()
        {
            _orderTransaction = await _orderContext.Database.BeginTransactionAsync();
            _profileTransaction = await _profileContext.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            await _orderTransaction?.CommitAsync()!;
            await _profileTransaction?.CommitAsync()!;
        }

        public async Task RollbackTransactionAsync()
        {
            await _orderTransaction?.RollbackAsync()!;
            await _profileTransaction?.RollbackAsync()!;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var result = await _profileContext.SaveChangesAsync(cancellationToken);
            result += await _orderContext.SaveChangesAsync(cancellationToken);
            return result;
        }
    }
}
