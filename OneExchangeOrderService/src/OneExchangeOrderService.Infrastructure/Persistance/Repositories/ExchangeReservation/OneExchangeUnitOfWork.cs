using OneExchangeOrderService.Application.Interfaces;
using OneExchangeOrderService.Domain.Persistence.Repositories.ExchangeReservation;
using OneExchangeOrderService.Infrastructure.DbContexts;

namespace OneExchangeOrderService.Infrastructure.Persistance.Repositories.ExchangeReservation
{
    public class OneExchangeUnitOfWork : IOneExchangeUnitOfWork
    {
        private readonly OneExchangeDbContext _context;

        public OneExchangeUnitOfWork(OneExchangeDbContext context)
        {
            _context = context;
            Orders = new OneExchangeOrderRepository(_context);  // Same context
            Profiles = new OneExchangeProfileRepository(_context); // Same context
        }

        public IOneExchangeOrderRepository Orders { get; }
        public IOneExchangeProfileRepository Profiles { get; }

        public Task<int> SaveAsync(CancellationToken ct)
            => _context.SaveChangesAsync(ct);
    }

}
