using InventoryService.Models;
using TestOpenTelemetry.Services;
using UserService.Models;

namespace TestOpenTelemetry.GraphQL.Queries;

public class GatewayQuery
{
    public async Task<IEnumerable<InventoryItem>> GetInventoryItems([Service] DownstreamGraphQLClient client, CancellationToken ct)
    {
        const string query = """
        query {
          inventoryItems {
            id
            name
            quantity
            createdAt
          }
        }
        """;

        var result = await client.PostAsync<InventoryItemsResult>("inventory-downstream", query, null, ct);
        return result?.InventoryItems ?? Enumerable.Empty<InventoryItem>();
    }

    public async Task<InventoryItem?> GetInventoryItemById(Guid id, [Service] DownstreamGraphQLClient client, CancellationToken ct)
    {
        const string query = """
        query ($id: ID!) {
          inventoryItemById(id: $id) {
            id
            name
            quantity
            createdAt
          }
        }
        """;

        var result = await client.PostAsync<InventoryItemByIdResult>("inventory-downstream", query, new { id }, ct);
        return result?.InventoryItemById;
    }

    public async Task<IEnumerable<User>> GetUsers([Service] DownstreamGraphQLClient client, CancellationToken ct)
    {
        const string query = """
        query {
          users {
            id
            name
            email
            createdAt
          }
        }
        """;

        var result = await client.PostAsync<UsersResult>("users-downstream", query, null, ct);
        return result?.Users ?? Enumerable.Empty<User>();
    }

    public async Task<User?> GetUserById(Guid id, [Service] DownstreamGraphQLClient client, CancellationToken ct)
    {
        const string query = """
        query ($id: ID!) {
          userById(id: $id) {
            id
            name
            email
            createdAt
          }
        }
        """;

        var result = await client.PostAsync<UserByIdResult>("users-downstream", query, new { id }, ct);
        return result?.UserById;
    }

    private sealed class InventoryItemsResult
    {
        public List<InventoryItem> InventoryItems { get; set; } = new();
    }

    private sealed class InventoryItemByIdResult
    {
        public InventoryItem? InventoryItemById { get; set; }
    }

    private sealed class UsersResult
    {
        public List<User> Users { get; set; } = new();
    }

    private sealed class UserByIdResult
    {
        public User? UserById { get; set; }
    }
}
