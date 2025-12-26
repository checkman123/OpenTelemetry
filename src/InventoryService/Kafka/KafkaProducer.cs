using System.Diagnostics;
using System.Text.Json;
using Confluent.Kafka;
using InventoryService.Models;
using InventoryService.Observability;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InventoryService.Kafka;

public class KafkaProducer : IDisposable
{
    private readonly KafkaOptions _options;
    private readonly ILogger<KafkaProducer> _logger;
    private readonly ActivitySource _activitySource;
    private readonly IProducer<string, string> _producer;

    public KafkaProducer(IOptions<KafkaOptions> options, ILogger<KafkaProducer> logger, ActivitySource activitySource)
    {
        _options = options.Value;
        _logger = logger;
        _activitySource = activitySource;

        var config = new ProducerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            ClientId = "inventory-service-producer",
            Acks = Acks.All,
            EnableIdempotence = true
        };

        _producer = new ProducerBuilder<string, string>(config)
            .SetErrorHandler((_, error) => _logger.LogError("Kafka producer error: {Reason}", error.Reason))
            .Build();
    }

    public async Task PublishInventoryItemAddedAsync(InventoryItem item, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(new
        {
            eventType = "InventoryItemAdded",
            itemId = item.Id,
            name = item.Name,
            quantity = item.Quantity,
            timestamp = DateTimeOffset.UtcNow
        });

        using var activity = _activitySource.StartActivity("Kafka Publish", ActivityKind.Producer);
        activity?.SetTag("messaging.system", "kafka");
        activity?.SetTag("messaging.destination", _options.Topic);
        activity?.SetTag("messaging.destination_kind", "topic");
        activity?.SetTag("messaging.kafka.bootstrap_servers", _options.BootstrapServers);

        try
        {
            var result = await _producer.ProduceAsync(_options.Topic, new Message<string, string>
            {
                Key = item.Id.ToString(),
                Value = payload
            }, cancellationToken);

            activity?.SetTag("messaging.kafka.offset", result.Offset.Value);
            _logger.LogInformation("Published inventory item {ItemId} to Kafka topic {Topic}", item.Id, _options.Topic);
        }
        catch (ProduceException<string, string> ex)
        {
            activity?.SetTag("exception.type", ex.GetType().FullName);
            activity?.SetTag("exception.message", ex.Message);
            _logger.LogError(ex, "Failed to publish inventory item {ItemId}", item.Id);
            throw;
        }
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}
