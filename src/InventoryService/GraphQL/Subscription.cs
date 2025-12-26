using HotChocolate;
using HotChocolate.Types;
using InventoryService.Models;

namespace InventoryService.GraphQL;

public class Subscription
{
    [Subscribe]
    [Topic(GraphQlTopics.InventoryItemAdded)]
    public InventoryItem OnInventoryItemAdded([EventMessage] InventoryItem item) => item;
}
