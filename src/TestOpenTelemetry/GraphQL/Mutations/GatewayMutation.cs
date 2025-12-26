using InventoryService.Models;
using TestOpenTelemetry.Services;
using UserService.Models;
using System.Text.Json.Serialization;

namespace TestOpenTelemetry.GraphQL.Mutations;

public class GatewayMutation
{
    public async Task<InventoryItem?> AddInventoryItem(string name, int quantity, [Service] DownstreamGraphQLClient client, CancellationToken ct)
    {
        const string mutation = """
        mutation ($name: String!, $quantity: Int!) {
          addInventoryItem(name: $name, quantity: $quantity) {
            id
            name
            quantity
            createdAt
          }
        }
        """;

        var result = await client.PostAsync<AddInventoryItemResult>("inventory-downstream", mutation, new { name, quantity }, ct);
        return result?.AddInventoryItem;
    }

    public async Task<User?> AddUser(string name, string email, [Service] DownstreamGraphQLClient client, CancellationToken ct)
    {
        const string mutation = """
        mutation ($name: String!, $email: String!) {
          addUser(name: $name, email: $email) {
            id
            name
            email
            createdAt
          }
        }
        """;

        var result = await client.PostAsync<AddUserResult>("users-downstream", mutation, new { name, email }, ct);
        return result?.AddUser;
    }

    private sealed class AddInventoryItemResult
    {
        [JsonPropertyName("addInventoryItem")]
        public InventoryItem? AddInventoryItem { get; set; }
    }

    private sealed class AddUserResult
    {
        [JsonPropertyName("addUser")]
        public User? AddUser { get; set; }
    }
}
