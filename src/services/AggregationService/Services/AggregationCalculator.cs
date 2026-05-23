using Contracts.Events;

namespace AggregationService.Services;

public static class AggregationCalculator
{
    // Groups tips by tip_type and calculates percentages within each type.
    // e.g. { "over_under": { "over_2.5": 0.80, "under_2.5": 0.20 }, "btts": { "yes": 1.0 } }
    public static Dictionary<string, Dictionary<string, double>> Calculate(
        IReadOnlyList<ProcessedTipEvent> tips)
    {
        var result = new Dictionary<string, Dictionary<string, double>>();

        var byType = tips.GroupBy(t => t.TipType);

        foreach (var typeGroup in byType)
        {
            var typeTotal = typeGroup.Count();
            var valueCounts = typeGroup
                .GroupBy(t => t.TipValue)
                .ToDictionary(
                    g => g.Key,
                    g => Math.Round((double)g.Count() / typeTotal, 4));

            result[typeGroup.Key] = valueCounts;
        }

        return result;
    }

    public static List<TipDetail> ToTipDetails(IReadOnlyList<ProcessedTipEvent> tips) =>
        tips.Select(t => new TipDetail
        {
            ChannelId    = t.ChannelId,
            ChannelTitle = t.ChannelTitle,
            TipType      = t.TipType,
            TipValue     = t.TipValue,
            Odds         = t.Odds
        }).ToList();
}
