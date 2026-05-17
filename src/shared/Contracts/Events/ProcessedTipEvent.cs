namespace Contracts.Events;

public record ProcessedTipEvent
{
    public Guid   TipId        { get; init; }
    public long   ChannelId    { get; init; }
    public string ChannelTitle { get; init; } = string.Empty;
    public int    MatchId      { get; init; }
    public string HomeTeam     { get; init; } = string.Empty;
    public string AwayTeam     { get; init; } = string.Empty;
    public string League       { get; init; } = string.Empty;
    public DateTimeOffset KickOff { get; init; }

    // "over_under" | "btts" | "1x2" | "handicap"
    public string TipType  { get; init; } = string.Empty;
    // "over_2.5" | "yes" | "home" | "draw" | "away" | ...
    public string TipValue { get; init; } = string.Empty;
    public decimal? Odds       { get; init; }
    public double  Confidence  { get; init; } // 0.0 – 1.0

    public long   RawMessageId { get; init; }
    public string Source       { get; init; } = "telegram";
    public DateTimeOffset Timestamp { get; init; }
}
