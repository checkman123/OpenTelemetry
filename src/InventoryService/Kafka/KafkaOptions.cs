namespace InventoryService.Kafka;

public class KafkaOptions
{
    public string BootstrapServers { get; set; } = "localhost:9092";

    public string Topic { get; set; } = "inventory-events";

    public string GroupId { get; set; } = "inventory-service";
}
