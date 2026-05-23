namespace MatchDataService.Configuration;

public class FootballApiOptions
{
    public const string Section = "FootballApi";

    public string Provider { get; set; } = "football-data";
    public string ApiKey   { get; set; } = string.Empty;
    public string BaseUrl  { get; set; } = "https://api.football-data.org/v4";
    public List<string> Leagues { get; set; } = ["PL", "PD", "SA", "BL1", "FL1"];
}
