using Microsoft.EntityFrameworkCore;
using PartnerCommission.Partners.Api;
using PartnerCommission.Partners.Api.Data;
using PartnerCommission.Partners.Api.Services;
using PartnerCommission.Shared.Data;
using PartnerCommission.Shared.Hosting;

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

builder.Services.AddDbContext<PartnersDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 5))
    );

builder.Services.AddOptions<PartnersOptions>()
    .BindConfiguration(PartnersOptions.Section)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddScoped<UserTreeQueries>();
builder.Services.AddScoped<IUserService, UsersService>();

var app = builder.Build();

await app.Services.MigrateWithLockAsync<PartnersDbContext>();

app.UseServiceDefaults();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapDefaultEndpoints();

app.MapControllers();

app.Run();
