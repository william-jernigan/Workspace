using Microsoft.EntityFrameworkCore;
using OneExchangeOrderService.Domain.Entities.ExchangeOrder;
using OneExchangeOrderService.Domain.Entities.ExchangeProfile;
using OneExchangeOrderService.Infrastructure.EFGenerator.Entities.Configurations.ExchangeProfile;
using OneExchangeOrderService.Infrastructure.Persistance.Configurations.ExchangeOrder;
using OneExchangeOrderService.Infrastructure.Persistance.Configurations.ExchangeProfile;

namespace OneExchangeOrderService.Infrastructure.DbContexts
{
    public class OneExchangeDbContext(DbContextOptions<OneExchangeDbContext> options) : DbContext(options)
    {

        // -------- Order ----------
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderGuest> OrderGuests { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<OrderItemAttribute> OrderItemAttributes { get; set; }
        public DbSet<OrderPayment> OrderPayments { get; set; }
        public DbSet<OrderRefund> OrderRefunds { get; set; }
        public DbSet<OrderReturn> OrderReturns { get; set; }
        public DbSet<ExchangeTransactionLog> ExchangeTransactionLogs { get; set; }

        // -------- Profile ----------
        public DbSet<Profile> Profiles { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Phone> Phones { get; set; }
        public DbSet<Identifier> Identifiers { get; set; }
        public DbSet<DigitalAddress> DigitalAddresses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Order configs
            modelBuilder.ApplyConfiguration(new OrderConfiguration());
            modelBuilder.ApplyConfiguration(new OrderGuestConfiguration());
            modelBuilder.ApplyConfiguration(new OrderItemConfiguration());
            modelBuilder.ApplyConfiguration(new OrderItemAttributeConfiguration());
            modelBuilder.ApplyConfiguration(new OrderPaymentConfiguration());
            modelBuilder.ApplyConfiguration(new OrderRefundConfiguration());
            modelBuilder.ApplyConfiguration(new OrderReturnConfiguration());
            modelBuilder.ApplyConfiguration(new ExchangeTransactionLogConfiguration());

            // Profile configs
            modelBuilder.ApplyConfiguration(new ProfileConfiguration());
            modelBuilder.ApplyConfiguration(new AddressConfiguration());
            modelBuilder.ApplyConfiguration(new PhoneConfiguration());
            modelBuilder.ApplyConfiguration(new IdentifierConfiguration());
            modelBuilder.ApplyConfiguration(new DigitalAddressConfiguration());
        }
    }
}
