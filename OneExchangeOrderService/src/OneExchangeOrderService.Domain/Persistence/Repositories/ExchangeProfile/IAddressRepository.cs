using OneExchangeOrderService.Domain.Entities.ExchangeProfile;

namespace OneExchangeOrderService.Domain.Persistence.Repositories.ExchangeProfile
{
    public interface IAddressRepository
    {
        public Task CreateAddressesAsync(List<Address> addresses);
    }
}
