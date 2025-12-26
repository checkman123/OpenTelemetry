using System.Diagnostics;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UserService.GraphQL.Mutations;
using UserService.GraphQL.Queries;
using UserService.Observability;
using UserService.Services;

namespace UserService;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUserModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<UserRepository>();
        services.TryAddSingleton(_ => new ActivitySource(TelemetryConstants.ActivitySourceName));
        return services;
    }

    public static IRequestExecutorBuilder AddUserGraphQLTypes(this IRequestExecutorBuilder builder)
    {
        builder
            .AddTypeExtension<UserQuery>()
            .AddTypeExtension<UserMutation>();
        return builder;
    }
}
