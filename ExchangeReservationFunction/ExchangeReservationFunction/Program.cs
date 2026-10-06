using ExchangeReservationFunction.Services;
using ExchangeReservationFunction.Services.Interfaces;
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
