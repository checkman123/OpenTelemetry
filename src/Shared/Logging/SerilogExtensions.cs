using System.Diagnostics;
using System.Reflection;
using Serilog;
using Serilog.Events;
using Shared.Logging;

namespace Shared.Logging;

public static class SerilogExtensions
{
    public static void ConfigureSerilog(this IHostBuilder hostBuilder, string serviceName)
    {
        hostBuilder.UseSerilog((context, services, loggerConfiguration) =>
        {
            var environment = context.HostingEnvironment.EnvironmentName;
            var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

            loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.With<ActivityEnricher>()
                .Enrich.WithProperty("service.name", serviceName)
                .Enrich.WithProperty("service.version", assemblyVersion)
                .Enrich.WithProperty("environment", environment);
        });
    }

    public static void UseRequestLogging(this WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.GetLevel = (httpContext, elapsedMs, exception) =>
            {
                if (exception != null || httpContext.Response.StatusCode >= 500)
                {
                    return LogEventLevel.Error;
                }

                if (httpContext.Response.StatusCode >= 400)
                {
                    return LogEventLevel.Warning;
                }

                return LogEventLevel.Information;
            };

            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("RequestProtocol", httpContext.Request.Protocol);
                diagnosticContext.Set("RequestId", httpContext.TraceIdentifier);
                diagnosticContext.Set("ConnectionId", httpContext.Connection.Id);

                var activity = Activity.Current;
                if (activity != null)
                {
                    diagnosticContext.Set("TraceId", activity.TraceId.ToString());
                    diagnosticContext.Set("SpanId", activity.SpanId.ToString());
                }
            };

            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        });
    }
}
