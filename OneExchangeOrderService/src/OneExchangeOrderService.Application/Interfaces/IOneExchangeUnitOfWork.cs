using OneExchangeOrderService.Domain.Persistence.Repositories.ExchangeReservation;

namespace OneExchangeOrderService.Application.Interfaces
{
    public interface IOneExchangeUnitOfWork
    {
        IOneExchangeOrderRepository Orders { get; }
        IOneExchangeProfileRepository Profiles { get; }
        Task<int> SaveAsync(CancellationToken ct);
    }
}
