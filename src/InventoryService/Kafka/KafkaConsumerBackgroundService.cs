using System.Diagnostics;
using Confluent.Kafka;
using InventoryService.Observability;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace InventoryService.Kafka;

public class KafkaConsumerBackgroundService : BackgroundService
{
    private readonly KafkaOptions _options;
    private readonly ILogger<KafkaConsumerBackgroundService> _logger;
    private readonly ActivitySource _activitySource;

    public KafkaConsumerBackgroundService(
        IOptions<KafkaOptions> options,
        ILogger<KafkaConsumerBackgroundService> logger,
        ActivitySource activitySource)
    {
        _options = options.Value;
        _logger = logger;
        _activitySource = activitySource;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        using var consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, e) => _logger.LogError("Kafka consumer error: {Reason}", e.Reason))
            .Build();

        consumer.Subscribe(_options.Topic);
        _logger.LogInformation("Kafka consumer subscribed to {Topic}", _options.Topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);
                if (result == null)
                {
                    continue;
                }

                using var activity = _activitySource.StartActivity("Kafka Consume", ActivityKind.Consumer);
                activity?.SetTag("messaging.system", "kafka");
                activity?.SetTag("messaging.destination", result.Topic);
                activity?.SetTag("messaging.destination_kind", "topic");
                activity?.SetTag("messaging.kafka.bootstrap_servers", _options.BootstrapServers);
                activity?.SetTag("messaging.kafka.partition", result.Partition.Value);
                activity?.SetTag("messaging.kafka.offset", result.Offset.Value);

                _logger.LogInformation("Consumed Kafka message key={Key} value={Value}", result.Message.Key, result.Message.Value);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error consuming Kafka message. Retrying...");
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }

        try
        {
            consumer.Close();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error closing Kafka consumer");
        }
    }
}
