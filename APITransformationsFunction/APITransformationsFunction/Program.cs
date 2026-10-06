using APITransformationsFunction.Services;
using APITransformationsFunction.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.IO;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration((hostingContext, configBuilder) =>
    {
        var env = hostingContext.HostingEnvironment;
        configBuilder.AddJsonFile(Path.Combine(env.ContentRootPath, "appsettings.json"), optional: true, reloadOnChange: false)
          .AddEnvironmentVariables();
    })
    .ConfigureServices((appBuilder, services) =>
    {
        //Add your services in DI here
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // TO DO - To be uncommented once the member service and catalog service is deployed

        var memberServiceSection = appBuilder.Configuration.GetSection("MemberService");
        services.AddHttpClient<IMemberService, MemberService>(client =>
        {
            client.BaseAddress = new Uri(memberServiceSection["BaseUrl"]);
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", memberServiceSection["SubscriptionKey"]);
            client.DefaultRequestHeaders.Add("version", (memberServiceSection["Version"] ?? "1").ToString());
        });

        var catalogServiceSection = appBuilder.Configuration.GetSection("CatalogService");
        services.AddHttpClient<IPropertyService, PropertyService>(client =>
        {
            client.BaseAddress = new Uri(catalogServiceSection["BaseUrl"]);
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", catalogServiceSection["SubscriptionKey"]);
            client.DefaultRequestHeaders.Add("version", (catalogServiceSection["Version"] ?? "1").ToString());
        });
        var inventoryServiceSection = appBuilder.Configuration.GetSection("InventoryService");
        services.AddHttpClient<IInventorySearchService, InventorySearchService>(client =>
        {
            client.BaseAddress = new Uri(inventoryServiceSection["BaseUrl"]);
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", inventoryServiceSection["SubscriptionKey"]);
            client.DefaultRequestHeaders.Add("version", (inventoryServiceSection["Version"] ?? "1").ToString());
            client.Timeout = TimeSpan.FromMinutes(3);
        });
        var orderServiceSection = appBuilder.Configuration.GetSection("OrderService");
        services.AddHttpClient<IReservationService, ReservationService>(client =>
        {
            client.BaseAddress = new Uri(orderServiceSection["BaseUrl"]);
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", orderServiceSection["SubscriptionKey"]);
            client.DefaultRequestHeaders.Add("version", (orderServiceSection["Version"] ?? "1").ToString());
        });
        services.AddHttpClient<ICancellationService, CancellationService>(client =>
        {
            client.BaseAddress = new Uri(orderServiceSection["BaseUrl"]);
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", orderServiceSection["SubscriptionKey"]);
            client.DefaultRequestHeaders.Add("version", (orderServiceSection["Version"] ?? "1").ToString());
        });
    })
    .UseSerilog((hostContext, services, configuration) => configuration.ReadFrom.Configuration(hostContext.Configuration))
    .Build();

host.Run();
