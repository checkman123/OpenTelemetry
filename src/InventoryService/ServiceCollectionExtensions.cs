using System.Diagnostics;
using HotChocolate.Execution.Configuration;
using InventoryService.GraphQL;
using InventoryService.GraphQL.Mutations;
using InventoryService.GraphQL.Queries;
using InventoryService.Kafka;
using InventoryService.Observability;
using InventoryService.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InventoryService;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInventoryModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KafkaOptions>(configuration.GetSection("Kafka"));
        services.AddSingleton<InventoryRepository>();
        services.AddSingleton<KafkaProducer>();
        services.AddHostedService<KafkaConsumerBackgroundService>();

        services.TryAddSingleton(_ => new ActivitySource(TelemetryConstants.ActivitySourceName));

        return services;
    }

    public static IRequestExecutorBuilder AddInventoryGraphQLTypes(this IRequestExecutorBuilder builder)
    {
        builder
            .AddTypeExtension<InventoryQuery>()
            .AddTypeExtension<InventoryMutation>()
            .AddTypeExtension<Subscription>();

        return builder;
    }
}
