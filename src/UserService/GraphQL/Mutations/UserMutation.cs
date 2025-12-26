using System.Diagnostics;
using HotChocolate;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.Logging;
using UserService.Models;
using UserService.Services;

namespace UserService.GraphQL.Mutations;

[ExtendObjectType("Mutation")]
public class UserMutation
{
    public User AddUser(
        string name,
        string email,
        [Service] UserRepository repository,
        [Service] ILogger<UserMutation> logger,
        [Service] ActivitySource activitySource,
        IResolverContext context)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new GraphQLException("Name must be provided.");
        }

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            throw new GraphQLException("Email must contain '@'.");
        }

        using var activity = activitySource.StartActivity("AddUser", ActivityKind.Internal);
        var operationName = context.Operation?.Name ?? "addUser";
        activity?.SetTag("operation.name", operationName);

        var user = repository.Add(name, email);

        logger.LogInformation("UserCreated {@UserId} {@Email} {@OperationName}", user.Id, user.Email, operationName);

        return user;
    }
}
