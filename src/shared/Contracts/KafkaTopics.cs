namespace Contracts;

public static class KafkaTopics
{
    public const string RawMessages       = "telegram.messages.raw";
    public const string ProcessedMessages = "telegram.messages.processed";
    public const string AggregatedFeed    = "feed.matches.aggregated";
    public const string DeadLetter        = "telegram.deadletter";
}
