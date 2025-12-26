using System.Collections.Concurrent;
using InventoryService.Models;
using Microsoft.Extensions.Logging;

namespace InventoryService.Services;

public class InventoryRepository
{
    private readonly ConcurrentDictionary<Guid, InventoryItem> _items = new();
    private readonly ILogger<InventoryRepository> _logger;

    public InventoryRepository(ILogger<InventoryRepository> logger)
    {
        _logger = logger;
    }

    public IEnumerable<InventoryItem> GetAll()
    {
        var items = _items.Values.ToList();
        _logger.LogDebug("GetAll module=inventory count={Count} items={Items}", items.Count, items);
        return items;
    }

    public InventoryItem? GetById(Guid id)
    {
        var found = _items.TryGetValue(id, out var item);

        _logger.LogDebug("GetById module=inventory id={Id} found={Found} item={Item}", id, found, item);
        return found ? item : null;
    }

    public InventoryItem Add(string name, int quantity)
    {
        var item = new InventoryItem
        {
            Id = Guid.NewGuid(),
            Name = name,
            Quantity = quantity
        };

        _items[item.Id] = item;

        _logger.LogDebug("Add module=inventory id={Id} name={Name} quantity={Quantity} item={Item}", item.Id, item.Name, item.Quantity, item);
        return item;
    }
}
