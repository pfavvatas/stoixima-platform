using System.Diagnostics;

namespace ApiGateway.Services;

public class ServiceHealthChecker
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _configuration;

    public ServiceHealthChecker(IHttpClientFactory httpFactory, IConfiguration configuration)
    {
        _httpFactory   = httpFactory;
        _configuration = configuration;
    }

    public async Task<List<ServiceStatusDto>> CheckAllAsync(CancellationToken ct)
    {
        var services = new[]
        {
            ("Telegram Gateway",    _configuration["Services:TelegramGateway"]  ?? "http://telegram-gateway:8081"),
            ("Storage Service",     _configuration["Services:StorageService"]   ?? "http://storage-service:8081"),
            ("Message Processing",  _configuration["Services:MessageProcessing"]?? "http://message-processing:8081"),
            ("Match Data Service",  _configuration["Services:MatchDataService"] ?? "http://match-data-service:8081"),
            ("Aggregation Service", _configuration["Services:AggregationService"]??"http://aggregation-service:8081"),
        };

        var tasks = services.Select(s => CheckOneAsync(s.Item1, s.Item2, ct));
        var results = await Task.WhenAll(tasks);
        return results.ToList();
    }

    private async Task<ServiceStatusDto> CheckOneAsync(string name, string baseUrl, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var client = _httpFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(3);
            var response = await client.GetAsync($"{baseUrl}/health", ct);
            sw.Stop();
            return new ServiceStatusDto
            {
                Name      = name,
                Status    = response.IsSuccessStatusCode ? "healthy" : "unhealthy",
                LatencyMs = (int)sw.ElapsedMilliseconds,
                CheckedAt = DateTimeOffset.UtcNow
            };
        }
        catch
        {
            return new ServiceStatusDto
            {
                Name      = name,
                Status    = "unreachable",
                LatencyMs = (int)sw.ElapsedMilliseconds,
                CheckedAt = DateTimeOffset.UtcNow
            };
        }
    }
}

public record ServiceStatusDto
{
    public string Name      { get; init; } = string.Empty;
    public string Status    { get; init; } = string.Empty;  // healthy | unhealthy | unreachable
    public int    LatencyMs { get; init; }
    public DateTimeOffset CheckedAt { get; init; }
}
