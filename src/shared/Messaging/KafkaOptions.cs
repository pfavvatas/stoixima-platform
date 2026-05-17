namespace Messaging;

public class KafkaOptions
{
    public const string Section = "Kafka";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string? GroupId         { get; set; }
    public int    MessageTimeoutMs { get; set; } = 5_000;
    public int    SessionTimeoutMs { get; set; } = 30_000;
}
