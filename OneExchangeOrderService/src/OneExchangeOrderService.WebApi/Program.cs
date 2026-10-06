using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Enterprise.Utilities.Swagger.DocumentFilters;
using Enterprise.Utilities.Swagger.NamedOptions;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using OneExchangeOrderService.Application.Eventhub;
using OneExchangeOrderService.Application.Interfaces;
using OneExchangeOrderService.Application.Interfaces.Logic;
using OneExchangeOrderService.Application.Logic;
using OneExchangeOrderService.Domain.Constants;
using OneExchangeOrderService.Domain.Helpers;
using OneExchangeOrderService.Domain.Persistence.Repositories.ExchangeOrder;
using OneExchangeOrderService.Domain.Persistence.Repositories.ExchangeProfile;
using OneExchangeOrderService.Infrastructure;
using OneExchangeOrderService.Infrastructure.Persistance.Repositories.ExchangeOrder;
using OneExchangeOrderService.WebApi.Formatter;
using OneExchangeOrderService.WebApi.Middlewares;
using Serilog;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

builder.Configuration
    .AddJsonFile("appsettings.json", true, true)
    .AddEnvironmentVariables()
    ;

// Configuration Wiki: https://dev.azure.com/HiltonGrandVacations/OneVision/_wiki/wikis/OneVision.wiki/1135/Configuration-Management?anchor=application-configuration
// 👇 Choose one of the options below to validate your configuration

//Configuration and validation using Data Annotations 👈
//builder.Services.AddOptions<YourOptions>()
//    .BindConfiguration("YourSectionName") //Bind your section to your strongly typed class
//    .ValidateDataAnnotations() // Enable Data Annotation Validation
//    .ValidateOnStart(); // Validate on startup

//Configuration and validation using FluentValidation (requires Enterprise.Extensions.FluentValidation package) 👈
//builder.Services.AddWithFluentValidation<YourOptions, YourOptionsValidator>("YourSectionName");

var oneExchangeDbConnectionString = builder.Configuration.GetConnectionString("OneExchangeConnectionString");

var schemaPath = Path.Combine(AppContext.BaseDirectory, "Schema", "common_reservation_schema.json");
var schemaJson = File.ReadAllText(schemaPath);
JsonSchemaValidator.InitializeReservationRequestSchema(schemaJson);


services.AddHealthChecks();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IContentfulService, ContentfulService>();
services.AddScoped<IExchangeTransactionLogRepository, ExchangeTransactionLogRepository>();


builder.Services.AddHeaderPropagation(o => o.Headers.Add("X_HGV_TransactionId", context => Guid.NewGuid().ToString()));
builder.Services.AddScoped<IInventoryServiceClient, InventoryServiceClient>();
builder.Services.AddScoped<IBoomiConnector, BoomiConnector>();
builder.Services.AddHttpClient(OneExchangeOrderServiceConstants.InventoryServiceClient, client =>
{
    client.BaseAddress = new Uri(builder.Configuration["InventoryService:BaseUrl"]);
    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", builder.Configuration["InventoryService:SubscriptionKey"]);
    client.DefaultRequestHeaders.Add("version", builder.Configuration["InventoryService:Version"] ?? "1");
    //client.Timeout = TimeSpan.FromSeconds(150);
});

builder.Services.AddScoped<IInRuleService, InRuleService>();
builder.Services.AddScoped<IOrderReservationEventPublisher, OrderReservationEventPublisher>();
services.AddScoped(sp => new OrderRepositories
{
    OrderRepository = sp.GetRequiredService<IOrderRepository>(),
    ProfileRepository = sp.GetRequiredService<IProfileRepository>(),
    OrderPaymentRepository = sp.GetRequiredService<IOrderPaymentRepository>(),
    ExchangeTransactionLogRepository = sp.GetRequiredService<IExchangeTransactionLogRepository>(),
    OrderReturnRepository = sp.GetRequiredService<IOrderReturnRepository>(),
    AddressRepository = sp.GetRequiredService<IAddressRepository>(),
    OrderRefundRepository = sp.GetRequiredService<IOrderRefundRepository>(),
    OrderItemAttributeRepository = sp.GetRequiredService<IOrderItemAttributeRepository>()
});
services.AddHttpClient(OneExchangeOrderServiceConstants.InRuleClient, client =>
{
    client.BaseAddress = new Uri(builder.Configuration["InRules:InRuleBaseUrl"]);
    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", builder.Configuration["InRules:SubscriptionKey"]);
});

services.AddHttpClient(OneExchangeOrderServiceConstants.BoomiConnectorClient, (sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var baseUrl = config["BoomiConnectorApi:BaseUrl"];
    var authHeader = config["BoomiConnectorApi:Authorization"];

    client.BaseAddress = new Uri(baseUrl ?? throw new InvalidOperationException("ReservationConnectorURL missing"));
    client.DefaultRequestHeaders.Add("Authorization", authHeader);
});

builder.Host.UseSerilog((hostContext, services, configuration) => configuration.ReadFrom.Configuration(builder.Configuration)
                .Enrich.WithCorrelationIdHeader("X_HGV_TransactionId"));

services.AddCors(o => o.AddPolicy("AllowAll", builder => builder.AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader()));

var logger = new LoggerFactory().CreateLogger(LogLevel.Debug.ToString());
services.AddSingleton((p) => logger);

services.AddHttpClient();
services.AddLogging();
services.AddControllers(options => options.InputFormatters.Insert(0, JsonPatchFormatter.GetJsonPatchInputFormatter()))
    .AddControllersAsServices()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });
services.AddRepositories(oneExchangeDbConnectionString);

//versioning
services.AddApiVersioning(options =>
{
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = ApiVersion.Default;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new HeaderApiVersionReader("version");
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});





services.AddEndpointsApiExplorer();
services.AddSwaggerGen(options =>
{
    options.DocumentFilter<HealthCheckFilter>();
    options.DescribeAllParametersInCamelCase();
    options.EnableAnnotations();
});
services.ConfigureOptions<SwaggerGenNamedOptions>();


// Turn on resilience by default
//services.ConfigureHttpClientDefaults(http =>
//{
//    http.AddStandardResilienceHandler(options =>
//    {
//        // override default 30s timeout
//        options.TotalRequestTimeout = new HttpTimeoutStrategyOptions
//        {
//            Timeout = TimeSpan.FromSeconds(30)
//        };
//    });
//});

// Turn on DI validation
builder.Host.UseDefaultServiceProvider(o =>
{
    o.ValidateOnBuild = true;
    o.ValidateScopes = true;
});

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
    foreach (var description in provider.ApiVersionDescriptions)
    {
        options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
    }
});

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseMiddleware<ErrorHandlerMiddleware>();

// 👇 Please configure your Azure App Connection string first and uncomment code below

app.MapControllers();

app.MapHealthChecks("health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();
