namespace ApiGateway.Models;

public record AggregatedMatchDto
{
    public int    MatchId      { get; init; }
    public string HomeTeam     { get; init; } = string.Empty;
    public string AwayTeam     { get; init; } = string.Empty;
    public string League       { get; init; } = string.Empty;
    public DateTimeOffset KickOff  { get; init; }
    public int    TotalTippers { get; init; }
    public string Status       { get; init; } = "scheduled";
    public Dictionary<string, Dictionary<string, double>> Consensus { get; init; } = new();
    public List<TipDetailDto> Tips { get; init; } = [];
    public DateTimeOffset UpdatedAt { get; init; }
}

public record TipDetailDto
{
    public long    ChannelId    { get; init; }
    public string  ChannelTitle { get; init; } = string.Empty;
    public string  TipType      { get; init; } = string.Empty;
    public string  TipValue     { get; init; } = string.Empty;
    public decimal? Odds        { get; init; }
}

public record MatchDto
{
    public int    Id         { get; init; }
    public string HomeTeam   { get; init; } = string.Empty;
    public string AwayTeam   { get; init; } = string.Empty;
    public string League     { get; init; } = string.Empty;
    public DateTimeOffset KickOff { get; init; }
    public string Status     { get; init; } = string.Empty;
    public int?   HomeScore  { get; init; }
    public int?   AwayScore  { get; init; }
}

public record ChannelDto
{
    public long   Id       { get; init; }
    public string Title    { get; init; } = string.Empty;
    public string Source   { get; init; } = string.Empty;
    public bool   Active   { get; init; }
    public int    TipCount { get; init; }
}
