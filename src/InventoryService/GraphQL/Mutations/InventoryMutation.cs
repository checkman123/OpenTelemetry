using HotChocolate;
using HotChocolate.Subscriptions;
using HotChocolate.Types;
using InventoryService.Kafka;
using InventoryService.Models;
using InventoryService.Services;

namespace InventoryService.GraphQL.Mutations;

[ExtendObjectType("Mutation")]
public class InventoryMutation
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
