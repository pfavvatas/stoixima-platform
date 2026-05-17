using MessageProcessing.Services;
using Xunit;

namespace MessageProcessing.Tests;

public class TipExtractorTests
{
    // ── Over / Under ──────────────────────────────────────────────────────────

    [Fact]
    public void OverUnder_Over25_WithOdds()
    {
        var r = TipExtractor.Extract("Over 2.5 goals @1.75");
        Assert.NotNull(r);
        Assert.Equal("over_under", r.TipType);
        Assert.Equal("over_2.5",   r.TipValue);
        Assert.Equal(1.75m,        r.Odds);
        Assert.True(r.Confidence >= 0.90);
    }

    [Fact]
    public void OverUnder_Under25_NoOdds()
    {
        var r = TipExtractor.Extract("Under 2.5 goals tonight ✅");
        Assert.NotNull(r);
        Assert.Equal("over_under", r.TipType);
        Assert.Equal("under_2.5",  r.TipValue);
        Assert.Null(r.Odds);
    }

    [Fact]
    public void OverUnder_Over15()
    {
        var r = TipExtractor.Extract("O 1.5 goals");
        Assert.NotNull(r);
        Assert.Equal("over_under", r.TipType);
        Assert.Equal("over_1.5",   r.TipValue);
    }

    [Fact]
    public void OverUnder_Over35()
    {
        var r = TipExtractor.Extract("OVER 3.5 @ 2.20");
        Assert.NotNull(r);
        Assert.Equal("over_3.5", r.TipValue);
        Assert.Equal(2.20m,      r.Odds);
    }

    [Fact]
    public void OverUnder_CaseInsensitive()
    {
        var r = TipExtractor.Extract("over 2.5");
        Assert.NotNull(r);
        Assert.Equal("over_2.5", r.TipValue);
    }

    // ── BTTS ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Btts_Yes_WithOdds()
    {
        var r = TipExtractor.Extract("Arsenal vs Chelsea BTTS Yes @1.85");
        Assert.NotNull(r);
        Assert.Equal("btts", r.TipType);
        Assert.Equal("yes",  r.TipValue);
        Assert.Equal(1.85m,  r.Odds);
        Assert.True(r.Confidence >= 0.90);
    }

    [Fact]
    public void Btts_GG_Slash()
    {
        var r = TipExtractor.Extract("GG / BTTS");
        Assert.NotNull(r);
        Assert.Equal("btts", r.TipType);
        Assert.Equal("yes",  r.TipValue);
    }

    [Fact]
    public void Btts_No()
    {
        var r = TipExtractor.Extract("BTTS No @2.10");
        Assert.NotNull(r);
        Assert.Equal("btts", r.TipType);
        Assert.Equal("no",   r.TipValue);
        Assert.Equal(2.10m,  r.Odds);
    }

    [Fact]
    public void Btts_NG()
    {
        var r = TipExtractor.Extract("NG @1.90");
        Assert.NotNull(r);
        Assert.Equal("btts", r.TipType);
        Assert.Equal("no",   r.TipValue);
    }

    [Fact]
    public void Btts_BothTeamsToScore()
    {
        var r = TipExtractor.Extract("Both teams to score Yes");
        Assert.NotNull(r);
        Assert.Equal("btts", r.TipType);
        Assert.Equal("yes",  r.TipValue);
    }

    // ── 1X2 ───────────────────────────────────────────────────────────────────

    [Fact]
    public void OneX2_HomeWin()
    {
        var r = TipExtractor.Extract("Home win @1.60");
        Assert.NotNull(r);
        Assert.Equal("1x2",  r.TipType);
        Assert.Equal("home", r.TipValue);
        Assert.Equal(1.60m,  r.Odds);
    }

    [Fact]
    public void OneX2_NumericHome()
    {
        var r = TipExtractor.Extract("1 @2.10");
        Assert.NotNull(r);
        Assert.Equal("1x2",  r.TipType);
        Assert.Equal("home", r.TipValue);
        Assert.Equal(2.10m,  r.Odds);
    }

    [Fact]
    public void OneX2_AwayWin()
    {
        var r = TipExtractor.Extract("Away win @3.50");
        Assert.NotNull(r);
        Assert.Equal("1x2",  r.TipType);
        Assert.Equal("away", r.TipValue);
    }

    [Fact]
    public void OneX2_Draw()
    {
        var r = TipExtractor.Extract("Draw @3.20");
        Assert.NotNull(r);
        Assert.Equal("1x2",  r.TipType);
        Assert.Equal("draw", r.TipValue);
    }

    // ── Handicap ──────────────────────────────────────────────────────────────

    [Fact]
    public void Handicap_NegativeOne()
    {
        var r = TipExtractor.Extract("Asian Handicap -1 @1.90");
        Assert.NotNull(r);
        Assert.Equal("handicap", r.TipType);
        Assert.Equal("-1",       r.TipValue);
        Assert.Equal(1.90m,      r.Odds);
    }

    [Fact]
    public void Handicap_PlusHalf()
    {
        var r = TipExtractor.Extract("Handicap +0.5 @1.75");
        Assert.NotNull(r);
        Assert.Equal("handicap", r.TipType);
        Assert.Equal("+0.5",     r.TipValue);
    }

    // ── Correct Score ─────────────────────────────────────────────────────────

    [Fact]
    public void CorrectScore_PrefixFormat()
    {
        var r = TipExtractor.Extract("Correct Score: 2-1 @7.00");
        Assert.NotNull(r);
        Assert.Equal("correct_score", r.TipType);
        Assert.Equal("2-1",           r.TipValue);
        Assert.Equal(7.00m,           r.Odds);
    }

    [Fact]
    public void CorrectScore_SuffixCS()
    {
        var r = TipExtractor.Extract("1-0 CS @5.50");
        Assert.NotNull(r);
        Assert.Equal("correct_score", r.TipType);
        Assert.Equal("1-0",           r.TipValue);
    }

    // ── Confidence ────────────────────────────────────────────────────────────

    [Fact]
    public void Confidence_HighWhenOddsPresent()
    {
        var r = TipExtractor.Extract("Over 2.5 @1.80");
        Assert.NotNull(r);
        Assert.True(r.Confidence >= 0.90);
    }

    [Fact]
    public void Confidence_LowerWithoutOdds()
    {
        var r = TipExtractor.Extract("Over 2.5");
        Assert.NotNull(r);
        Assert.True(r.Confidence < 0.90);
    }

    // ── Null / no match ───────────────────────────────────────────────────────

    [Fact]
    public void NoMatch_RandomText() => Assert.Null(TipExtractor.Extract("Great game last night!"));
    [Fact]
    public void NoMatch_Empty()      => Assert.Null(TipExtractor.Extract(""));
    [Fact]
    public void NoMatch_Null()       => Assert.Null(TipExtractor.Extract(null!));
}
