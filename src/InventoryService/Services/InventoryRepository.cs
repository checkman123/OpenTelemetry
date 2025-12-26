using System.Collections.Concurrent;
using System.Diagnostics;
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
        var sw = Stopwatch.StartNew();
        _logger.LogDebug("GetAll start module=inventory");

        var items = _items.Values.ToList();

        sw.Stop();
        _logger.LogDebug("GetAll end module=inventory count={Count} durationMs={DurationMs}", items.Count, sw.Elapsed.TotalMilliseconds);
        return items;
    }

    public InventoryItem? GetById(Guid id)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogDebug("GetById start module=inventory id={Id}", id);

        var found = _items.TryGetValue(id, out var item);

        sw.Stop();
        _logger.LogDebug("GetById end module=inventory id={Id} found={Found} durationMs={DurationMs}", id, found, sw.Elapsed.TotalMilliseconds);
        return found ? item : null;
    }

    public InventoryItem Add(string name, int quantity)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogDebug("Add start module=inventory name={Name} quantity={Quantity}", name, quantity);

        var item = new InventoryItem
        {
            Id = Guid.NewGuid(),
            Name = name,
            Quantity = quantity
        };

        _items[item.Id] = item;

        sw.Stop();
        _logger.LogDebug("Add end module=inventory id={Id} durationMs={DurationMs}", item.Id, sw.Elapsed.TotalMilliseconds);
        return item;
    }
}
