namespace Contracts.Events;

public record AggregatedMatchEvent
{
    public int    MatchId   { get; init; }
    public string HomeTeam  { get; init; } = string.Empty;
    public string AwayTeam  { get; init; } = string.Empty;
    public string League    { get; init; } = string.Empty;
    public DateTimeOffset KickOff { get; init; }

    public int TotalTippers { get; init; }

    // tip_type → (tip_value → percentage 0.0–1.0)
    // e.g. { "over_under": { "over_2.5": 0.80, "under_2.5": 0.20 }, "btts": { "yes": 1.0 } }
    public Dictionary<string, Dictionary<string, double>> Consensus { get; init; } = new();

    public List<TipDetail> Tips { get; init; } = new();

    public DateTimeOffset UpdatedAt { get; init; }
}

public record TipDetail
{
    public long    ChannelId    { get; init; }
    public string  ChannelTitle { get; init; } = string.Empty;
    public string  TipType      { get; init; } = string.Empty;
    public string  TipValue     { get; init; } = string.Empty;
    public decimal? Odds        { get; init; }
}
