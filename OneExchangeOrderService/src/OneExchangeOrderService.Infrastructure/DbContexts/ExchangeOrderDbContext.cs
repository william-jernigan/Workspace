
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OneExchangeOrderService.Application.Interfaces.Logic;
using OneExchangeOrderService.Domain.Entities.ExchangeOrder;
using OneExchangeOrderService.Infrastructure.Persistance.Configurations.ExchangeOrder;
using System.Data;
namespace OneExchangeOrderService.Infrastructure.DbContexts
{
    public partial class ExchangeOrderDbContext(DbContextOptions<ExchangeOrderDbContext> options) : DbContext(options), IExchangeOrderDbContext, IUnitOfWork
    {

        public virtual DbSet<ExchangeTransactionLog> ExchangeTransactionLogs { get; set; }

        public virtual DbSet<Order> Orders { get; set; }

        public virtual DbSet<OrderGuest> OrderGuests { get; set; }

        public virtual DbSet<OrderItem> OrderItems { get; set; }

        public virtual DbSet<OrderItemAttribute> OrderItemAttributes { get; set; }

        public virtual DbSet<OrderPayment> OrderPayments { get; set; }

        public virtual DbSet<OrderRefund> OrderRefunds { get; set; }

        public virtual DbSet<OrderReturn> OrderReturns { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new ExchangeTransactionLogConfiguration());
            modelBuilder.ApplyConfiguration(new OrderConfiguration());
            modelBuilder.ApplyConfiguration(new OrderGuestConfiguration());
            modelBuilder.ApplyConfiguration(new OrderItemConfiguration());
            modelBuilder.ApplyConfiguration(new OrderItemAttributeConfiguration());
            modelBuilder.ApplyConfiguration(new OrderPaymentConfiguration());
            modelBuilder.ApplyConfiguration(new OrderRefundConfiguration());
            modelBuilder.ApplyConfiguration(new OrderReturnConfiguration());

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
