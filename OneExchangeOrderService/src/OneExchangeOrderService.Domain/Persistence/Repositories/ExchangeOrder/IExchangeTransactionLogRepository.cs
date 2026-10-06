
using OneExchangeOrderService.Domain.Entities.ExchangeOrder;

namespace OneExchangeOrderService.Domain.Persistence.Repositories.ExchangeOrder
{
    public interface IExchangeTransactionLogRepository
    {
        Task<List<ExchangeTransactionLog>> GetTransactionLogsAsync(Guid tenantId, string customerExternalReferenceId);

        public Task CreateTransactionAsync(ExchangeTransactionLog transaction);
    }
}
