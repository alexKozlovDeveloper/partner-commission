using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using PartnerCommission.Commissions.Api.Background;
using PartnerCommission.Commissions.Api.Data;
using PartnerCommission.Commissions.Api.Services;
using PartnerCommission.Commissions.Domain;
using PartnerCommission.Shared.Exceptions;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Db")
    ?? throw new InvalidOperationException("Connection string 'Db' is not configured");

var partnersBaseAddress = builder.Configuration["Services:Partners"]
    ?? throw new InvalidOperationException("Services:Partners is not configured");

var walletsBaseAddress = builder.Configuration["Services:Wallets"]
    ?? throw new InvalidOperationException("Services:Wallets is not configured");

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services
    .AddHealthChecks()
    .AddNpgSql(connectionString, tags: ["ready"]);

builder.Services.AddDbContext<CommissionsDbContext>(options => 
    options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 5))
    );

builder.Services.AddScoped<ICommissionsService, CommissionsService>();
builder.Services.AddScoped<ICommissionSchemaSettings, CommissionSchemaSettings>();
builder.Services.AddScoped<ICommissionCalculator, CommissionCalculator>();

builder.Services
    .AddHttpClient<IPartnersClient, PartnersClient>(client => { client.BaseAddress = new Uri(partnersBaseAddress); })
    .AddStandardResilienceHandler();

builder.Services
    .AddHttpClient<IWalletsClient, WalletsClient>(client => { client.BaseAddress = new Uri(walletsBaseAddress); })
    .AddStandardResilienceHandler();

//builder.Services.AddSingleton<CommissionCalculator>();
builder.Services.AddScoped<ProfitEventHandler>();
builder.Services.AddScoped<OutboxMessageHandler>();

builder.Services.AddHostedService<ProfitEventProcessor>();
builder.Services.AddHostedService<OutboxDispatcher>();

builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = c => c.Tags.Contains("ready")
});

app.MapControllers();

app.Run();
