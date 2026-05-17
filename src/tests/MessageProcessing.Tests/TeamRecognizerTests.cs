using MessageProcessing.Services;
using Xunit;

namespace MessageProcessing.Tests;

// Stub that lets tests inject a fixed team list without a Postgres connection.
file sealed class StubTeamCache(IReadOnlyList<TeamEntry> teams) : ITeamCache
{
    public IReadOnlyList<TeamEntry> Teams => teams;
}

public class TeamRecognizerTests
{
    // ── Fixture ───────────────────────────────────────────────────────────────

    private static readonly IReadOnlyList<TeamEntry> SeedTeams =
    [
        new(1, "Arsenal",           ["AFC", "Gunners", "Arsenal FC"],          "England", "Premier League"),
        new(2, "Manchester United", ["Man Utd", "MUFC", "Man United", "United"],"England", "Premier League"),
        new(3, "Chelsea",           ["The Blues", "CFC", "Chelsea FC"],         "England", "Premier League"),
        new(4, "Manchester City",   ["Man City", "MCFC", "City"],               "England", "Premier League"),
        new(5, "Liverpool",         ["LFC", "The Reds", "Liverpool FC"],        "England", "Premier League"),
    ];

    private static TeamRecognizer Build() =>
        new(new StubTeamCache(SeedTeams));

    // ── Exact name match ──────────────────────────────────────────────────────

    [Fact] public void ExactName_Arsenal() => Assert.Equal(1, Build().Recognize("Arsenal"));
    [Fact] public void ExactName_Lowercase() => Assert.Equal(1, Build().Recognize("arsenal"));
    [Fact] public void ExactName_Uppercase() => Assert.Equal(1, Build().Recognize("ARSENAL"));
    [Fact] public void ExactName_ManchesterUnited() => Assert.Equal(2, Build().Recognize("Manchester United"));
    [Fact] public void ExactName_Chelsea() => Assert.Equal(3, Build().Recognize("Chelsea"));
    [Fact] public void ExactName_Liverpool() => Assert.Equal(5, Build().Recognize("Liverpool"));

    // ── Alias match ───────────────────────────────────────────────────────────

    [Fact] public void Alias_AFC() => Assert.Equal(1, Build().Recognize("AFC"));
    [Fact] public void Alias_Gunners() => Assert.Equal(1, Build().Recognize("Gunners"));
    [Fact] public void Alias_ManUtd() => Assert.Equal(2, Build().Recognize("Man Utd"));
    [Fact] public void Alias_MUFC() => Assert.Equal(2, Build().Recognize("MUFC"));
    [Fact] public void Alias_ManUnited() => Assert.Equal(2, Build().Recognize("Man United"));
    [Fact] public void Alias_ManCity() => Assert.Equal(4, Build().Recognize("Man City"));
    [Fact] public void Alias_TheBlues() => Assert.Equal(3, Build().Recognize("The Blues"));
    [Fact] public void Alias_LFC() => Assert.Equal(5, Build().Recognize("LFC"));
    [Fact] public void Alias_CaseInsensitive() => Assert.Equal(2, Build().Recognize("man utd"));

    // ── Fuzzy match (typos ≤ 2 characters) ───────────────────────────────────

    [Fact] public void Fuzzy_ArsnalMissingE() => Assert.Equal(1, Build().Recognize("Arsnal"));
    [Fact] public void Fuzzy_ArenalTransposed() => Assert.Equal(1, Build().Recognize("Arenal"));
    [Fact] public void Fuzzy_ArsenalExtraL() => Assert.Equal(1, Build().Recognize("Arsenall"));
    [Fact] public void Fuzzy_ChelseaMissingA() => Assert.Equal(3, Build().Recognize("Chelsae"));
    [Fact] public void Fuzzy_LiverpoolMissingO() => Assert.Equal(5, Build().Recognize("Liverpol"));

    // ── No match ──────────────────────────────────────────────────────────────

    [Fact] public void NoMatch_Empty() => Assert.Null(Build().Recognize(""));
    [Fact] public void NoMatch_Null() => Assert.Null(Build().Recognize(null));
    [Fact] public void NoMatch_UnknownTeam() => Assert.Null(Build().Recognize("Real Madrid"));
    [Fact] public void NoMatch_PSG() => Assert.Null(Build().Recognize("PSG"));
}
