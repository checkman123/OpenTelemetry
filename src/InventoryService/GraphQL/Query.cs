using HotChocolate;
using InventoryService.Models;
using InventoryService.Services;

namespace InventoryService.GraphQL;

public class Query
{
    public IEnumerable<InventoryItem> GetInventoryItems([Service] InventoryRepository repository)
        => repository.GetAll();

    public InventoryItem? GetInventoryItemById(Guid id, [Service] InventoryRepository repository)
        => repository.GetById(id);
}
