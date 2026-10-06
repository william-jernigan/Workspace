
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OneExchangeOrderService.Application.Interfaces.Logic;
using OneExchangeOrderService.Domain.Entities.Exchange;
using OneExchangeOrderService.Infrastructure.Persistance.Configurations.Exchange;
using System.Data;
namespace OneExchangeOrderService.Infrastructure.DbContexts
{
    public partial class ExchangeDbContext(DbContextOptions<ExchangeDbContext> options) : DbContext(options), IExchangeDbContext, IUnitOfWork
    {

        public virtual DbSet<Organization> Organizations { get; set; }

        public virtual DbSet<Partner> Partners { get; set; }

        public virtual DbSet<Tenant> Tenants { get; set; }

        public virtual DbSet<TradingAgreement> TradingAgreements { get; set; }

        public IDbTransaction BeginTransaction()
        {
            var transaction = Database.BeginTransaction();
            return transaction.GetDbTransaction();
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new OrganizationConfiguration());
            modelBuilder.ApplyConfiguration(new PartnerConfiguration());
            modelBuilder.ApplyConfiguration(new TenantConfiguration());
            modelBuilder.ApplyConfiguration(new TradingAgreementConfiguration());

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
