using HotChocolate.AspNetCore;
using HotChocolate.Diagnostics;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Shared.Logging;
using UserService;
using UserService.Observability;

const string ServiceName = "user-service";

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();
builder.Host.ConfigureSerilog(ServiceName);

builder.Services.AddUserModule(builder.Configuration);
builder.Services.AddHealthChecks();

var serviceVersion = typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0";
var environment = builder.Environment.EnvironmentName;
var otlpEndpoint = builder.Configuration.GetValue<string>("OpenTelemetry:Endpoint")
    ?? builder.Configuration.GetValue<string>("OTEL_EXPORTER_OTLP_ENDPOINT")
    ?? "http://localhost:4317";

var resourceBuilder = ResourceBuilder.CreateDefault()
    .AddService(serviceName: ServiceName, serviceVersion: serviceVersion, serviceInstanceId: Environment.MachineName)
    .AddAttributes(new[]
    {
        new KeyValuePair<string, object>("deployment.environment", environment)
    });

builder.Services.AddOpenTelemetry()
    .ConfigureResource(rb => rb.AddService(serviceName: ServiceName, serviceVersion: serviceVersion, serviceInstanceId: Environment.MachineName)
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

builder.Services.AddLogging(logging => logging.AddOpenTelemetry(options =>
{
    options.IncludeFormattedMessage = true;
    options.IncludeScopes = true;
    options.ParseStateValues = true;
    options.SetResourceBuilder(resourceBuilder);
    options.AddOtlpExporter(exporterOptions => exporterOptions.Endpoint = new Uri(otlpEndpoint));
}));

builder.Services
    .AddGraphQLServer()
    .AddQueryType(d => d.Name("Query"))
    .AddMutationType(d => d.Name("Mutation"))
    .AddUserGraphQLTypes()
    .AddInstrumentation(options =>
    {
        options.RenameRootActivity = true;
        options.Scopes = ActivityScopes.All;
    });

var app = builder.Build();

app.UseRequestLogging();
app.UseWebSockets();

app.MapGraphQL("/graphql");
if (app.Environment.IsDevelopment())
{
    app.MapBananaCakePop("/graphql/ui", "/graphql");
}
app.MapGet("/healthz", () => Results.Ok(new { status = "healthy" }));

var logger = Log.ForContext<Program>();
app.Lifetime.ApplicationStarted.Register(() =>
{
    logger.Information("Application started {ServiceName} listening on {Urls}", ServiceName, string.Join(", ", app.Urls));
});
app.Lifetime.ApplicationStopping.Register(() => logger.Information("Application stopping {ServiceName}", ServiceName));
app.Lifetime.ApplicationStopped.Register(() => logger.Information("Application stopped {ServiceName}", ServiceName));

app.Run();
