using System.Data;

namespace OneExchangeOrderService.Application.Interfaces.Logic
{
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        IDbTransaction BeginTransaction();
    }
}
