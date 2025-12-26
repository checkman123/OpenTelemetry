using System.Collections.Concurrent;
using InventoryService.Models;

namespace InventoryService.Services;

public class InventoryRepository
{
    private readonly ConcurrentDictionary<Guid, InventoryItem> _items = new();

    public IEnumerable<InventoryItem> GetAll() => _items.Values;

    public InventoryItem? GetById(Guid id) => _items.TryGetValue(id, out var item) ? item : null;

    public InventoryItem Add(string name, int quantity)
    {
        var item = new InventoryItem
        {
            Id = Guid.NewGuid(),
            Name = name,
            Quantity = quantity
        };

        _items[item.Id] = item;

        return item;
    }
}
