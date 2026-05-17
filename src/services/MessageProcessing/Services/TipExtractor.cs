using System.Text.RegularExpressions;

namespace MessageProcessing.Services;

public static class TipExtractor
{
    // ── Odds ──────────────────────────────────────────────────────────────────
    // Matches: @1.85 | odds: 1.85 | [1.85] | (1.85) | standalone 1.50–15.00
    private static readonly Regex OddsPattern = new(
        @"(?:@|odds\s*:\s*|\[|\()?\s*(?<odds>[1-9]\d{0,1}\.\d{2})\s*(?:\]|\))?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // ── Over/Under ────────────────────────────────────────────────────────────
    private static readonly Regex OverUnderPattern = new(
        @"\b(?<dir>over|under|o|u)\s*(?<line>[0-9]+\.?[05]?)\s*(?:goals?)?\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // ── BTTS (Both Teams To Score) ────────────────────────────────────────────
    private static readonly Regex BttsPattern = new(
        @"\b(?:btts|gg|both\s+teams?\s+(?:to\s+)?score)\s*(?<val>yes|no|/)?\b" +
        @"|\b(?<ng>ng|no\s+goal)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // ── 1X2 ───────────────────────────────────────────────────────────────────
    private static readonly Regex OneX2Pattern = new(
        @"\b(?<sel>1x2|home\s+win|away\s+win|draw|home|away)\b" +
        @"|\b(?<num>[12])\s*@" +    // "1 @2.10" or "2 @1.95"
        @"|\bx\b",                  // lone X = draw
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // ── Handicap ──────────────────────────────────────────────────────────────
    private static readonly Regex HandicapPattern = new(
        @"\b(?:asian\s+)?handicap\s*(?<hcap>[+-]?\d+(?:\.\d+)?)\b" +
        @"|\b(?<hcap2>[+-]\d+(?:\.\d+)?)\s*handicap\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // ── Correct Score ─────────────────────────────────────────────────────────
    private static readonly Regex CorrectScorePattern = new(
        @"\b(?:correct\s+score|cs|score)\s*:?\s*(?<h>\d)-(?<a>\d)\b" +
        @"|\b(?<h2>\d)-(?<a2>\d)\s+(?:cs|correct\s+score)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // ── Public API ────────────────────────────────────────────────────────────

    // Returns null when no recognisable tip pattern is found.
    public static TipExtractionResult? Extract(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        decimal? odds = ExtractOdds(text);

        if (TryExtractOverUnder(text, odds, out var ou)) return ou;
        if (TryExtractBtts(text, odds, out var btts))   return btts;
        if (TryExtractCorrectScore(text, odds, out var cs)) return cs;
        if (TryExtractHandicap(text, odds, out var hc)) return hc;
        if (TryExtract1x2(text, odds, out var ox))     return ox;

        return null;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static decimal? ExtractOdds(string text)
    {
        foreach (Match m in OddsPattern.Matches(text))
        {
            if (decimal.TryParse(m.Groups["odds"].Value,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var o) && o >= 1.01m && o <= 50m)
                return o;
        }
        return null;
    }

    private static bool TryExtractOverUnder(string text, decimal? odds, out TipExtractionResult? result)
    {
        var m = OverUnderPattern.Match(text);
        if (!m.Success) { result = null; return false; }

        var dir  = m.Groups["dir"].Value.ToLowerInvariant();
        var line = m.Groups["line"].Value;

        var direction = dir is "over" or "o" ? "over" : "under";
        var value     = $"{direction}_{line}";

        result = new TipExtractionResult
        {
            TipType    = "over_under",
            TipValue   = value,
            Odds       = odds,
            Confidence = odds.HasValue ? 0.95 : 0.80
        };
        return true;
    }

    private static bool TryExtractBtts(string text, decimal? odds, out TipExtractionResult? result)
    {
        var m = BttsPattern.Match(text);
        if (!m.Success) { result = null; return false; }

        string value;
        if (m.Groups["ng"].Success)
            value = "no";
        else
        {
            var raw = m.Groups["val"].Value.ToLowerInvariant();
            value = raw is "" or "/" ? "yes" : raw;
        }

        result = new TipExtractionResult
        {
            TipType    = "btts",
            TipValue   = value,
            Odds       = odds,
            Confidence = odds.HasValue ? 0.95 : 0.85
        };
        return true;
    }

    private static bool TryExtract1x2(string text, decimal? odds, out TipExtractionResult? result)
    {
        var m = OneX2Pattern.Match(text);
        if (!m.Success) { result = null; return false; }

        string value;
        if (m.Groups["sel"].Success)
        {
            value = m.Groups["sel"].Value.ToLowerInvariant() switch
            {
                "home win" or "home" => "home",
                "away win" or "away" => "away",
                _                    => "draw"
            };
        }
        else if (m.Groups["num"].Success)
        {
            value = m.Groups["num"].Value == "1" ? "home" : "away";
        }
        else
        {
            value = "draw";
        }

        result = new TipExtractionResult
        {
            TipType    = "1x2",
            TipValue   = value,
            Odds       = odds,
            Confidence = odds.HasValue ? 0.90 : 0.70
        };
        return true;
    }

    private static bool TryExtractHandicap(string text, decimal? odds, out TipExtractionResult? result)
    {
        var m = HandicapPattern.Match(text);
        if (!m.Success) { result = null; return false; }

        var hcap = m.Groups["hcap"].Success
            ? m.Groups["hcap"].Value
            : m.Groups["hcap2"].Value;

        result = new TipExtractionResult
        {
            TipType    = "handicap",
            TipValue   = hcap,
            Odds       = odds,
            Confidence = odds.HasValue ? 0.90 : 0.75
        };
        return true;
    }

    private static bool TryExtractCorrectScore(string text, decimal? odds, out TipExtractionResult? result)
    {
        var m = CorrectScorePattern.Match(text);
        if (!m.Success) { result = null; return false; }

        var h = m.Groups["h"].Success  ? m.Groups["h"].Value  : m.Groups["h2"].Value;
        var a = m.Groups["a"].Success  ? m.Groups["a"].Value  : m.Groups["a2"].Value;

        result = new TipExtractionResult
        {
            TipType    = "correct_score",
            TipValue   = $"{h}-{a}",
            Odds       = odds,
            Confidence = odds.HasValue ? 0.90 : 0.80
        };
        return true;
    }
}
