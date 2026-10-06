
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OneExchangeOrderService.Application.Interfaces.Logic;
using OneExchangeOrderService.Domain.Entities.ExchangeProfile;
using OneExchangeOrderService.Infrastructure.EFGenerator.Entities.Configurations.ExchangeProfile;
using OneExchangeOrderService.Infrastructure.Persistance.Configurations.ExchangeProfile;
using System.Data;
namespace OneExchangeOrderService.Infrastructure.DbContexts
{
    public partial class ExchangeProfileDbContext(DbContextOptions<ExchangeProfileDbContext> options) : DbContext(options), IExchangeProfileDbContext, IUnitOfWork
    {

        public virtual DbSet<Address> Addresses { get; set; }

        public virtual DbSet<DigitalAddress> DigitalAddresses { get; set; }

        public virtual DbSet<Identifier> Identifiers { get; set; }

        public virtual DbSet<Phone> Phones { get; set; }

        public virtual DbSet<Profile> Profiles { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new AddressConfiguration());
            modelBuilder.ApplyConfiguration(new DigitalAddressConfiguration());
            modelBuilder.ApplyConfiguration(new IdentifierConfiguration());
            modelBuilder.ApplyConfiguration(new PhoneConfiguration());
            modelBuilder.ApplyConfiguration(new ProfileConfiguration());

            OnModelCreatingPartial(modelBuilder);
        }
        public IDbTransaction BeginTransaction()
        {
            var transaction = Database.BeginTransaction();
            return transaction.GetDbTransaction();
        }
        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
