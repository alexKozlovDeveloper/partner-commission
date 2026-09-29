using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using PartnerCommission.Shared.Diagnostics;
using PartnerCommission.Shared.Exceptions;
using Prometheus;
using System.Diagnostics;

namespace PartnerCommission.Shared.Hosting;

public static class ServiceDefaultsExtensions
{
    public static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(10);

    public const string LivePath = "/health/live";
    public const string ReadyPath = "/health/ready";

    public const string ReadyTag = "ready";

    public static WebApplicationBuilder AddServiceDefaults(this WebApplicationBuilder builder)
    {
        Tracing.EnsureListenerRegistered();

        AddLogging(builder);

        builder.Services.Configure<HostOptions>(o => o.ShutdownTimeout = ShutdownTimeout);

        builder.Services.AddProblemDetails(o => o.CustomizeProblemDetails =
            ctx => ctx.ProblemDetails.Extensions.TryAdd("traceId", Activity.Current?.Id ?? ctx.HttpContext.TraceIdentifier));

        builder.Services.AddExceptionHandler<ApiExceptionHandler>();

        builder.Services.AddHealthChecks().ForwardToPrometheus();

        return builder;
    }

    public static WebApplication UseServiceDefaults(this WebApplication app)
    {
        app.UseHttpMetrics();

        app.UseExceptionHandler();

        return app;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        app.MapHealthChecks(LivePath, new HealthCheckOptions
        {
            Predicate = _ => false
        });

        app.MapHealthChecks(ReadyPath, new HealthCheckOptions
        {
            Predicate = c => c.Tags.Contains(ReadyTag)
        });

        app.MapMetrics();

        return app;
    }

    private static void AddLogging(WebApplicationBuilder builder)
    {
        builder.Logging.Configure(o => o.ActivityTrackingOptions =
            ActivityTrackingOptions.TraceId | ActivityTrackingOptions.SpanId | ActivityTrackingOptions.ParentId);

        builder.Services.Configure<ConsoleLoggerOptions>(o => o.FormatterName ??= ConsoleFormatterNames.Simple);

        builder.Services.Configure<SimpleConsoleFormatterOptions>(o =>
        {
            o.IncludeScopes = true;
            o.SingleLine = true;
            o.TimestampFormat = "HH:mm:ss ";
        });

        builder.Services.Configure<JsonConsoleFormatterOptions>(o =>
        {
            o.IncludeScopes = true;
            o.UseUtcTimestamp = true;
            o.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
        });

        builder.Logging.AddFilter("Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware", LogLevel.None);
    }
}
