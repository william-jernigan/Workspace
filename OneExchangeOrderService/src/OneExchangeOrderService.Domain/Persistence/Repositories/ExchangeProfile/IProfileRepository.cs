using OneExchangeOrderService.Domain.Entities.Common;
using OneExchangeOrderService.Domain.Entities.ExchangeProfile;
using OneExchangeOrderService.Domain.Models;
using OneExchangeOrderService.Domain.Persistence.Repositories.Common;

namespace OneExchangeOrderService.Domain.Persistence.Repositories.ExchangeProfile
{
    public interface IProfileRepository : IBaseRepository<BaseEntity>
    {
        public Task<string> CreateProfileAsync(Profile profile);
        public Task<IEnumerable<GuestDetails>> GetGuestsAsync(List<Guid> profileIds);
        public Task<IEnumerable<Address>> GetProfileAddressAsync(string addressType, Guid? billProfileId);
        public Task<IEnumerable<Profile>> GetOrderGuestsAsync(List<Guid> profileIds);
        public Task<IEnumerable<Profile>> GetOrderGuestsDetailsAsync(List<Guid> profileIds);
        public Task<IEnumerable<Profile>> GetProfilesByExternalReferenceIdAsync(string externalReferenceId);
    }
}
