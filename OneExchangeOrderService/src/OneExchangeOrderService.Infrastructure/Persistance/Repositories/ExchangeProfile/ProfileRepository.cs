using Microsoft.EntityFrameworkCore;
using OneExchangeOrderService.Domain.Constants;
using OneExchangeOrderService.Domain.Entities.Common;
using OneExchangeOrderService.Domain.Entities.ExchangeProfile;
using OneExchangeOrderService.Domain.Models;
using OneExchangeOrderService.Domain.Persistence.Repositories.ExchangeProfile;
using OneExchangeOrderService.Infrastructure.DbContexts;

namespace OneExchangeOrderService.Infrastructure.Persistance.Repositories.ExchangeProfile
{
    public sealed class ProfileRepository(ExchangeProfileDbContext context) : BaseExchangeOrderProfileRepository<BaseEntity>(context), IProfileRepository
    {
        /// <summary>
        /// To get guests details.
        /// </summary>
        /// <param name="profileIds"></param>
        /// <returns></returns>
        public async Task<IEnumerable<GuestDetails>> GetGuestsAsync(List<Guid> profileIds)
        {
            var guests = await context.Profiles.AsNoTracking().
                Where(x => profileIds.Contains(x.Id))
              .Select(guest => new GuestDetails
              {
                  ProfileId = guest.Id,
                  FirstName = guest.FirstName,
                  LastName = guest.LastName,
                  Address = guest.Addresses.FirstOrDefault().Line1 ?? guest.Addresses.FirstOrDefault().Line2 ?? guest.Addresses.FirstOrDefault().Line3 ?? string.Empty,
                  City = guest.Addresses.FirstOrDefault().City ?? string.Empty,
                  State = guest.Addresses.FirstOrDefault().State ?? string.Empty,
                  Country = guest.Addresses.FirstOrDefault().Country ?? string.Empty,
                  PostalCode = guest.Addresses.FirstOrDefault().Postalcode ?? string.Empty,
                  PhoneNumber = guest.Phones.FirstOrDefault().Number ?? string.Empty,
                  EmailAddress = guest.DigitalAddresses.FirstOrDefault(x => x.DigitalAddressType == OneExchangeOrderServiceConstants.GuestEmail).DigitalAddress1 ?? string.Empty,
              }).ToListAsync();

            return guests;
        }

        public async Task<string> CreateProfileAsync(Profile profile)
        {
            Context.Profiles.Add(profile);
            return profile.ProfileType;
        }

        /// <summary>
        /// To get address details
        /// </summary>
        /// <param name="addressType"></param>
        /// <param name="billProfileId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Address>> GetProfileAddressAsync(string addressType, Guid? billProfileId)
        {
            var billingAddress = await context.Addresses.AsNoTracking().
                Where(x => x.ProfileId == billProfileId && x.AddressType == addressType)
                .TagWith(nameof(GetProfileAddressAsync)).ToListAsync();

            return billingAddress;
        }

        /// <summary>
        ///  To get guest informations
        /// </summary>
        /// <param name="profileIds"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Profile>> GetOrderGuestsAsync(List<Guid> profileIds)
        {
            var guests = await context.Profiles.AsNoTracking().Where(x => profileIds.Contains(x.Id))
                 .TagWith(nameof(GetOrderGuestsAsync)).ToListAsync();

            return guests;
        }

        /// <summary>
        ///  To get guest informations with eager loading
        /// </summary>
        /// <param name="profileIds"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Profile>> GetOrderGuestsDetailsAsync(List<Guid> profileIds)
        {
            var guests = await context.Profiles
                .AsNoTracking()
                .Include(p => p.Addresses)
                .Include(p => p.Phones)
                .Include(p => p.DigitalAddresses)
                .Where(x => profileIds.Contains(x.Id))
                .TagWith(nameof(GetOrderGuestsAsync))
                .ToListAsync();

            return guests;
        }

        public async Task<IEnumerable<Profile>> GetProfilesByExternalReferenceIdAsync(string externalReferenceId) => await context.Profiles
                .AsNoTracking()
                .Where(p => p.ExternalReferenceId == externalReferenceId)
                .ToListAsync();
    }
}
