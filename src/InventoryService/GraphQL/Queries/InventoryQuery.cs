using HotChocolate;
using HotChocolate.Types;
using InventoryService.Models;
using InventoryService.Services;

namespace InventoryService.GraphQL.Queries;

[ExtendObjectType("Query")]
public class InventoryQuery
{
    public IEnumerable<InventoryItem> GetInventoryItems([Service] InventoryRepository repository)
        => repository.GetAll();

    public InventoryItem? GetInventoryItemById(Guid id, [Service] InventoryRepository repository)
        => repository.GetById(id);
}
