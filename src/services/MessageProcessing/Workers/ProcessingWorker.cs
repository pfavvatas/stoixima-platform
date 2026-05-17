namespace MessageProcessing.Workers;

// Full implementation in #11 (team recognition) and #12 (tip extraction).
public class ProcessingWorker : BackgroundService
{
    private readonly ILogger<ProcessingWorker> _logger;

    public ProcessingWorker(ILogger<ProcessingWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ProcessingWorker starting — tip extraction pending (#11, #12).");
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
