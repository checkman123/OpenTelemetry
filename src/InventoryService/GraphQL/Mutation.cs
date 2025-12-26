using HotChocolate;
using HotChocolate.Subscriptions;
using InventoryService.Models;
using InventoryService.Services;
using InventoryService.Kafka;

namespace InventoryService.GraphQL;

public class Mutation
{
    public async Task<InventoryItem> AddInventoryItem(
        string name,
        int quantity,
        [Service] InventoryRepository repository,
        [Service] KafkaProducer producer,
        [Service] ITopicEventSender eventSender)
    {
        var item = repository.Add(name, quantity);

        await producer.PublishInventoryItemAddedAsync(item);
        await eventSender.SendAsync(GraphQlTopics.InventoryItemAdded, item);

        return item;
    }
}
