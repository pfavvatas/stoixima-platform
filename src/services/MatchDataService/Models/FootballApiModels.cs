using System.Text.Json.Serialization;

namespace MatchDataService.Models;

public record FootballApiResponse
{
    [JsonPropertyName("matches")]
    public List<ApiMatch> Matches { get; init; } = [];
}

public record ApiMatch
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("competition")]
    public ApiCompetition Competition { get; init; } = new();

    [JsonPropertyName("utcDate")]
    public DateTimeOffset UtcDate { get; init; }

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("homeTeam")]
    public ApiTeam HomeTeam { get; init; } = new();

    [JsonPropertyName("awayTeam")]
    public ApiTeam AwayTeam { get; init; } = new();

    [JsonPropertyName("score")]
    public ApiScore? Score { get; init; }

    [JsonPropertyName("season")]
    public ApiSeason? Season { get; init; }
}

public record ApiCompetition
{
    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
}

public record ApiTeam
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("shortName")]
    public string ShortName { get; init; } = string.Empty;

    [JsonPropertyName("tla")]
    public string Tla { get; init; } = string.Empty;
}

public record ApiScore
{
    [JsonPropertyName("fullTime")]
    public ApiScoreDetail? FullTime { get; init; }
}

public record ApiScoreDetail
{
    [JsonPropertyName("home")]
    public int? Home { get; init; }

    [JsonPropertyName("away")]
    public int? Away { get; init; }
}

public record ApiSeason
{
    [JsonPropertyName("startDate")]
    public string StartDate { get; init; } = string.Empty;

    [JsonPropertyName("endDate")]
    public string EndDate { get; init; } = string.Empty;
}
