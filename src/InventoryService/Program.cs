using HotChocolate.AspNetCore;
using HotChocolate.Diagnostics;
using InventoryService.GraphQL;
using InventoryService.Kafka;
using InventoryService.Observability;
using InventoryService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.Configure<KafkaOptions>(builder.Configuration.GetSection("Kafka"));
builder.Services.AddSingleton<InventoryRepository>();
builder.Services.AddSingleton<KafkaProducer>();
builder.Services.AddHostedService<KafkaConsumerBackgroundService>();

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddSubscriptionType<Subscription>()
    .AddInMemorySubscriptions()
    .AddInstrumentation(options =>
    {
        options.RenameRootActivity = true;
        options.Scopes = ActivityScopes.All;
    });

builder.Services.AddOpenTelemetryServices(builder.Configuration);
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseWebSockets();
app.MapGraphQL("/graphql");
app.MapBananaCakePop("/graphql/ui");
app.MapGet("/healthz", () => Results.Ok(new { status = "healthy" }));

app.Run();
