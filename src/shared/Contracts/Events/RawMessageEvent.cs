namespace Contracts.Events;

public record RawMessageEvent
{
    public long   MessageId      { get; init; }
    public long   ChatId         { get; init; }
    public string ChatTitle      { get; init; } = string.Empty;
    public long   SenderId       { get; init; }
    public string SenderUsername { get; init; } = string.Empty;
    public string MessageText    { get; init; } = string.Empty;
    public bool   HasMedia       { get; init; }
    public string MediaType      { get; init; } = string.Empty; // "photo", "document", ""
    public string MediaFileId    { get; init; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; }
    public string Source         { get; init; } = "telegram";
    public Guid   AccountId      { get; init; }
}
