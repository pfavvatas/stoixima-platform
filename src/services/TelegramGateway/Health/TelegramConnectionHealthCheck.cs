using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TelegramGateway.Health;

// Checks whether the Telegram connection is currently active.
// The TelegramWorker sets IsConnected when the client is authenticated.
public class TelegramConnectionHealthCheck : IHealthCheck
{
    public static bool IsConnected { get; set; }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            IsConnected
                ? HealthCheckResult.Healthy("Telegram connection active.")
                : HealthCheckResult.Degraded("Telegram client not yet connected."));
    }
}
