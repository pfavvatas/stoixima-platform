using Prometheus;
using Tesseract;

namespace MessageProcessing.Services;

public class OcrService
{
    private readonly ILogger<OcrService> _logger;
    private readonly string _tessDataPath;

    private static readonly Counter OcrCallsTotal = Metrics.CreateCounter(
        "processing_ocr_calls_total", "Total OCR processing attempts");

    private static readonly Histogram OcrDuration = Metrics.CreateHistogram(
        "processing_ocr_duration_ms", "OCR processing duration in ms",
        new HistogramConfiguration { Buckets = Histogram.ExponentialBuckets(50, 2, 8) });

    public OcrService(IConfiguration configuration, ILogger<OcrService> logger)
    {
        _logger       = logger;
        _tessDataPath = configuration["Ocr:TessDataPath"] ?? "./tessdata";
    }

    // Returns OCR-extracted text, or null if extraction fails or image is unreadable.
    public string? ExtractText(byte[] imageBytes)
    {
        OcrCallsTotal.Inc();
        using var _ = OcrDuration.NewTimer();

        try
        {
            using var engine = new TesseractEngine(_tessDataPath, "eng", EngineMode.Default);
            engine.SetVariable("tessedit_char_whitelist",
                "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789.,@-:/ ()");

            using var pix  = Pix.LoadFromMemory(imageBytes);
            using var page = engine.Process(pix);

            var text = page.GetText()?.Trim();
            if (string.IsNullOrWhiteSpace(text))
                return null;

            _logger.LogDebug("OCR extracted {Length} chars", text.Length);
            return text;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OCR failed — continuing without image text");
            return null;
        }
    }
}
