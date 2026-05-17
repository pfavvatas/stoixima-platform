using FuzzySharp;

namespace MessageProcessing.Services;

public class TeamRecognizer
{
    private readonly ITeamCache _cache;
    private const int FuzzyThreshold = 80;

    public TeamRecognizer(ITeamCache cache) => _cache = cache;

    // Returns the team ID if the input matches a known team name or alias,
    // either exactly (case-insensitive) or via fuzzy matching (≥ 80% score).
    // Returns null if no team is recognised above the threshold.
    public int? Recognize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var query = input.Trim();
        var teams = _cache.Teams;

        // ── 1. Exact match (case-insensitive) ────────────────────────────────
        foreach (var team in teams)
        {
            if (string.Equals(team.Name, query, StringComparison.OrdinalIgnoreCase))
                return team.Id;

            foreach (var alias in team.Aliases)
                if (string.Equals(alias, query, StringComparison.OrdinalIgnoreCase))
                    return team.Id;
        }

        // ── 2. Fuzzy match ────────────────────────────────────────────────────
        var queryLower = query.ToLowerInvariant();
        int   bestScore  = 0;
        int?  bestTeamId = null;

        foreach (var team in teams)
        {
            foreach (var candidate in team.Aliases.Prepend(team.Name))
            {
                var score = Fuzz.WeightedRatio(queryLower, candidate.ToLowerInvariant());
                if (score > bestScore)
                {
                    bestScore  = score;
                    bestTeamId = team.Id;
                }
            }
        }

        return bestScore >= FuzzyThreshold ? bestTeamId : null;
    }
}
