using OneExchangeOrderService.Domain.Entities.ExchangeProfile;

namespace OneExchangeOrderService.Domain.Persistence.Repositories.ExchangeReservation
{
    public interface IOneExchangeProfileRepository
    {
        Task CreateProfileAsync(Profile profile);
    }
}
