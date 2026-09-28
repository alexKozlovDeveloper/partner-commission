using Microsoft.EntityFrameworkCore;
using PartnerCommission.Partners.Api;
using PartnerCommission.Partners.Api.Data;
using PartnerCommission.Partners.Api.Services;
using PartnerCommission.Shared.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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

app.UseServiceDefaults();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapDefaultEndpoints();

app.MapControllers();

app.Run();
