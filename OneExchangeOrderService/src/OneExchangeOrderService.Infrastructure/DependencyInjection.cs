
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OneExchangeOrderService.Application.Eventhub;
using OneExchangeOrderService.Application.Interfaces;
using OneExchangeOrderService.Application.Interfaces.Logic;
using OneExchangeOrderService.Domain.Persistence.Repositories.ExchangeOrder;
using OneExchangeOrderService.Domain.Persistence.Repositories.ExchangeProfile;
using OneExchangeOrderService.Infrastructure.DbContexts;
using OneExchangeOrderService.Infrastructure.Persistance.Repositories.ExchangeOrder;
using OneExchangeOrderService.Infrastructure.Persistance.Repositories.ExchangeProfile;
using OneExchangeOrderService.Infrastructure.Persistance.Repositories.ExchangeReservation;

namespace OneExchangeOrderService.Infrastructure
{
    public static class DependencyInjection
    {
        public static void AddRepositories(this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<ExchangeDbContext>((sp, options) => options.UseSqlServer(connectionString));
            services.AddDbContext<ExchangeOrderDbContext>((sp, options) => options.UseSqlServer(connectionString));
            services.AddDbContext<ExchangeProfileDbContext>((sp, options) => options.UseSqlServer(connectionString));
            services.AddScoped<IExchangeDbContext>(sp => sp.GetRequiredService<ExchangeDbContext>());
            services.AddScoped<IExchangeProfileDbContext>(sp => sp.GetRequiredService<ExchangeProfileDbContext>());
            services.AddScoped<IExchangeOrderDbContext>(sp => sp.GetRequiredService<ExchangeOrderDbContext>());

            //Add all your repositories DI registration here
            //services.AddScoped<IPartyRepository, PartyRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IProfileRepository, ProfileRepository>();
            services.AddScoped<IOrderPaymentRepository, OrderPaymentRepository>();
            services.AddScoped<IOrderReturnRepository, OrderReturnRepository>();
            services.AddScoped<IOrderRefundRepository, OrderRefundRepository>();
            services.AddScoped<IExchangeTransactionLogRepository, ExchangeTransactionLogRepository>();
            services.AddScoped<IOrderItemAttributeRepository, OrderItemAttributeRepository>();
            services.AddScoped<IOrderUnitOfWork, OrderUnitOfWork>();
            services.AddSingleton<OrderReservationEventPublisher>();
            services.AddScoped<IAddressRepository, AddressRepository>();
            services.AddScoped<IInRuleService, InRuleService>();
            services.AddSingleton<OrderCancelEventPublisher>();
            services.AddDbContext<OneExchangeDbContext>((sp, options) =>
            options.UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(2), null)));
            services.AddScoped<IOneExchangeUnitOfWork, OneExchangeUnitOfWork>();
        }
    }
}
