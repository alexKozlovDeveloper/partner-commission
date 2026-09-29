using Microsoft.EntityFrameworkCore;
using PartnerCommission.Commissions.Api.Background;
using PartnerCommission.Commissions.Api.Data;
using PartnerCommission.Commissions.Api.Services;
using PartnerCommission.Commissions.Domain;
using PartnerCommission.Shared.Data;
using PartnerCommission.Shared.Hosting;
using Prometheus;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString("Db")
    ?? throw new InvalidOperationException("Connection string 'Db' is not configured");

var partnersBaseAddress = builder.Configuration["Services:Partners"]
    ?? throw new InvalidOperationException("Services:Partners is not configured");

var walletsBaseAddress = builder.Configuration["Services:Wallets"]
    ?? throw new InvalidOperationException("Services:Wallets is not configured");

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services
    .AddHealthChecks()
    .AddNpgSql(connectionString, tags: [ServiceDefaultsExtensions.ReadyTag]);

builder.Services.AddDbContext<CommissionsDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 5))
    );

builder.Services.AddScoped<ICommissionsService, CommissionsService>();
builder.Services.AddScoped<ICommissionSchemaSettings, CommissionSchemaSettings>();
builder.Services.AddSingleton<ICommissionCalculator, CommissionCalculator>();

var partnersHttpClient = builder.Services
    .AddHttpClient<IPartnersClient, PartnersClient>(client => { client.BaseAddress = new Uri(partnersBaseAddress); });

partnersHttpClient.AddStandardResilienceHandler();
partnersHttpClient.UseHttpClientMetrics();

var walletsHttpClient = builder.Services
    .AddHttpClient<IWalletsClient, WalletsClient>(client => { client.BaseAddress = new Uri(walletsBaseAddress); });

walletsHttpClient.AddStandardResilienceHandler();
walletsHttpClient.UseHttpClientMetrics();

builder.Services.AddScoped<ProfitEventHandler>();
builder.Services.AddScoped<OutboxMessageHandler>();

builder.Services.AddPollingJob<ProfitEventProcessor>();
builder.Services.AddPollingJob<OutboxDispatcher>();

var app = builder.Build();

await app.Services.MigrateWithLockAsync<CommissionsDbContext>();
await CommissionsDbSeeder.SeedAsync(app.Services);

app.UseServiceDefaults();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapDefaultEndpoints();

app.MapControllers();

app.Run();
