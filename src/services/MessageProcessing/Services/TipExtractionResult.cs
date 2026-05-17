namespace MessageProcessing.Services;

public record TipExtractionResult
{
    public string  TipType    { get; init; } = string.Empty; // over_under | btts | 1x2 | handicap | correct_score
    public string  TipValue   { get; init; } = string.Empty; // e.g. "over_2.5", "yes", "home", "-1", "2-1"
    public decimal? Odds      { get; init; }
    public double  Confidence { get; init; }                  // 0.0 – 1.0
}
