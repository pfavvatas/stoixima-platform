using System.Text.RegularExpressions;
using Contracts;
using Contracts.Events;
using Messaging;
using Prometheus;

namespace MessageProcessing.Services;

public class MessageProcessor
{
    // Splits "Arsenal vs Chelsea", "Real Madrid - Barcelona", "Man City v Liverpool"
    private static readonly Regex TeamSplitPattern = new(
        @"\s+(?:vs\.?|v\.?|-|:)\s+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly OcrService _ocr;
    private readonly TeamRecognizer _teamRecognizer;
    private readonly MatchCorrelator _matchCorrelator;
    private readonly TipPersister _tipPersister;
    private readonly IEventPublisher _publisher;
    private readonly ILogger<MessageProcessor> _logger;

    private static readonly Counter TipsExtractedTotal = Metrics.CreateCounter(
        "processing_tips_extracted_total", "Successfully extracted and persisted tips");

    private static readonly Counter UnmatchedTotal = Metrics.CreateCounter(
        "processing_unmatched_messages_total", "Messages that couldn't be matched to a known match");

    public MessageProcessor(
        OcrService ocr,
        TeamRecognizer teamRecognizer,
        MatchCorrelator matchCorrelator,
        TipPersister tipPersister,
        IEventPublisher publisher,
        ILogger<MessageProcessor> logger)
    {
        _ocr            = ocr;
        _teamRecognizer = teamRecognizer;
        _matchCorrelator = matchCorrelator;
        _tipPersister   = tipPersister;
        _publisher      = publisher;
        _logger         = logger;
    }

    public async Task ProcessAsync(RawMessageEvent raw, byte[]? imageBytes, CancellationToken ct)
    {
        // 1. Build full text (message text + optional OCR output)
        var text = raw.MessageText ?? string.Empty;
        if (imageBytes is { Length: > 0 })
        {
            var ocrText = _ocr.ExtractText(imageBytes);
            if (!string.IsNullOrEmpty(ocrText))
                text = $"{text}\n{ocrText}".Trim();
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            UnmatchedTotal.Inc();
            return;
        }

        // 2. Extract tip first (cheap, fail-fast before DB lookups)
        var tipResult = TipExtractor.Extract(text);
        if (tipResult is null)
        {
            _logger.LogDebug("No tip pattern found in message {MessageId}", raw.MessageId);
            UnmatchedTotal.Inc();
            return;
        }

        // 3. Team recognition — try to find two teams in the message
        var (homeId, awayId) = ExtractTeamPair(text);
        if (homeId is null || awayId is null)
        {
            _logger.LogDebug("Could not recognise team pair in message {MessageId}", raw.MessageId);
            UnmatchedTotal.Inc();
            return;
        }

        // 4. Match correlation
        var match = await _matchCorrelator.FindAsync(homeId.Value, awayId.Value, ct);
        if (match is null)
        {
            _logger.LogDebug("No match found for teams ({Home}, {Away})", homeId, awayId);
            UnmatchedTotal.Inc();
            return;
        }

        // 5. Build ProcessedTipEvent
        var tip = new ProcessedTipEvent
        {
            TipId         = Guid.NewGuid(),
            ChannelId     = raw.ChatId,
            ChannelTitle  = raw.ChatTitle,
            MatchId       = match.Id,
            HomeTeam      = match.HomeTeam,
            AwayTeam      = match.AwayTeam,
            League        = match.League,
            KickOff       = match.KickOff,
            TipType       = tipResult.TipType,
            TipValue      = tipResult.TipValue,
            Odds          = tipResult.Odds,
            Confidence    = tipResult.Confidence,
            RawMessageId  = raw.MessageId,
            Source        = raw.Source,
            Timestamp     = DateTimeOffset.UtcNow,
        };

        // 6. Persist
        await _tipPersister.PersistAsync(tip, ct);

        // 7. Publish
        await _publisher.PublishAsync(KafkaTopics.ProcessedMessages, raw.ChatId.ToString(), tip, ct);

        TipsExtractedTotal.Inc();
        _logger.LogInformation(
            "Tip extracted: {TipType}/{TipValue} for {Home} vs {Away} (match {MatchId})",
            tip.TipType, tip.TipValue, tip.HomeTeam, tip.AwayTeam, tip.MatchId);
    }

    // Tries patterns like "TeamA vs TeamB" and "TeamA - TeamB".
    // Falls back to scanning the whole text for any recognisable team.
    private (int? homeId, int? awayId) ExtractTeamPair(string text)
    {
        var parts = TeamSplitPattern.Split(text, 2);
        if (parts.Length == 2)
        {
            var h = _teamRecognizer.Recognize(parts[0].Trim());
            var a = _teamRecognizer.Recognize(parts[1].Split('\n')[0].Trim());
            if (h.HasValue && a.HasValue && h != a)
                return (h, a);
        }

        // Fallback: check each line for a team name pair
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var lineParts = TeamSplitPattern.Split(line, 2);
            if (lineParts.Length < 2) continue;
            var h = _teamRecognizer.Recognize(lineParts[0].Trim());
            var a = _teamRecognizer.Recognize(lineParts[1].Trim());
            if (h.HasValue && a.HasValue && h != a)
                return (h, a);
        }

        return (null, null);
    }
}
