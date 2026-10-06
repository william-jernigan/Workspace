using OneExchangeOrderService.Domain.Entities.ExchangeProfile;
using OneExchangeOrderService.Domain.Persistence.Repositories.ExchangeProfile;
using OneExchangeOrderService.Infrastructure.DbContexts;

namespace OneExchangeOrderService.Infrastructure.Persistance.Repositories.ExchangeProfile
{
    public class AddressRepository(ExchangeProfileDbContext context) : IAddressRepository
    {
        public async Task CreateAddressesAsync(List<Address> addresses) => await context.Addresses.AddRangeAsync(addresses);
    }
}
