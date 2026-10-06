using Microsoft.EntityFrameworkCore;
using OneExchangeOrderService.Domain.Entities.ExchangeOrder;
using OneExchangeOrderService.Domain.Persistence.Repositories.ExchangeOrder;
using OneExchangeOrderService.Infrastructure.DbContexts;

namespace OneExchangeOrderService.Infrastructure.Persistance.Repositories.ExchangeOrder
{
    public sealed class ExchangeTransactionLogRepository(ExchangeOrderDbContext context) : IExchangeTransactionLogRepository
    {
        public async Task<List<ExchangeTransactionLog>> GetTransactionLogsAsync(Guid tenantId, string customerExternalReferenceId) => await context.ExchangeTransactionLogs
                .Include(txn => txn.Order)
                    .ThenInclude(o => o.OrderItems)
                        .ThenInclude(oi => oi.OrderItemAttributes)
                .Include(txn => txn.Order)
                    .ThenInclude(o => o.OrderReturn)
                .Where(txn =>
                    txn.Order.TenantId == tenantId &&
                    txn.Order.CustomerExternalReferenceId == customerExternalReferenceId)
                .OrderByDescending(txn => txn.TransactionDate)
                .ToListAsync();
        public async Task CreateTransactionAsync(ExchangeTransactionLog transaction) => await context.ExchangeTransactionLogs.AddAsync(transaction);
    }
}
