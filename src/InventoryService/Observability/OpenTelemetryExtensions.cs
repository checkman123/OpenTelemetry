using System.Diagnostics;
using System.Reflection;
using HotChocolate.Diagnostics;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace InventoryService.Observability;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddOpenTelemetryServices(this IServiceCollection services, IConfiguration configuration)
    {
        var serviceName = configuration.GetValue<string>("OpenTelemetry:ServiceName") ?? "inventory-service";
        var serviceVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
        var environment = configuration.GetValue<string>("OpenTelemetry:DeploymentEnvironment") ?? "local";
        var otlpEndpoint = configuration.GetValue<string>("OpenTelemetry:Endpoint") ?? "http://localhost:4317";

        services.TryAddSingleton(_ => new ActivitySource(TelemetryConstants.ActivitySourceName));

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: serviceName, serviceVersion: serviceVersion, serviceInstanceId: Environment.MachineName)
                .AddAttributes(new[]
                {
                    new KeyValuePair<string, object>("deployment.environment", environment)
                }))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddHotChocolateInstrumentation()
                .AddSource(TelemetryConstants.ActivitySourceName)
                .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint)))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter("Microsoft.AspNetCore.Hosting")
                .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
                .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint)));

        services.AddLogging(logging => logging.AddOpenTelemetry(options =>
        {
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
            options.ParseStateValues = true;
            options.SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
                .AddAttributes(new[]
                {
                    new KeyValuePair<string, object>("deployment.environment", environment)
                }));
            options.AddOtlpExporter(exporterOptions => exporterOptions.Endpoint = new Uri(otlpEndpoint));
        }));

        return services;
    }
}
