using Microsoft.EntityFrameworkCore;
using PartnerCommission.Shared.Data;
using PartnerCommission.Shared.Hosting;
using PartnerCommission.Wallets.Api.Background;
using PartnerCommission.Wallets.Api.Data;
using PartnerCommission.Wallets.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("Db")
    ?? throw new InvalidOperationException("Connection string 'Db' is not configured");

builder.Services
    .AddHealthChecks()
    .AddNpgSql(connectionString, tags: [ServiceDefaultsExtensions.ReadyTag]);

builder.Services.AddDbContext<WalletsDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 5))
    );

builder.Services.AddScoped<IWalletsService, WalletsService>();

builder.Services.AddScoped<PayoutHandler>();

builder.Services.AddPollingJob<PayoutProcessor>();

var app = builder.Build();

await app.Services.MigrateWithLockAsync<WalletsDbContext>();

app.UseServiceDefaults();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapDefaultEndpoints();

app.MapControllers();

app.Run();
