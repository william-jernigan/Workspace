using OneExchangeOrderService.Domain.Entities.ExchangeProfile;
using OneExchangeOrderService.Domain.Persistence.Repositories.ExchangeReservation;
using OneExchangeOrderService.Infrastructure.DbContexts;

namespace OneExchangeOrderService.Infrastructure.Persistance.Repositories.ExchangeReservation
{
    public sealed class OneExchangeProfileRepository(OneExchangeDbContext context) : IOneExchangeProfileRepository
    {
        private readonly OneExchangeDbContext _context = context;

        public async Task CreateProfileAsync(Profile profile) => _context.Profiles.Add(profile);
    }
}
