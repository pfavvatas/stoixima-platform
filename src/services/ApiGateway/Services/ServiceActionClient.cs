namespace ApiGateway.Services;

public class ServiceActionClient
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ServiceActionClient> _logger;

    private static readonly Dictionary<string, string> ServiceUrls = new()
    {
        ["match-data-service"]  = "http://match-data-service:8081",
        ["aggregation-service"] = "http://aggregation-service:8081",
    };

    public ServiceActionClient(
        IHttpClientFactory httpFactory,
        IConfiguration configuration,
        ILogger<ServiceActionClient> logger)
    {
        _httpFactory   = httpFactory;
        _configuration = configuration;
        _logger        = logger;
    }

    public async Task<bool> TriggerAsync(string serviceName, string path, CancellationToken ct)
    {
        var configKey = $"Services:{ToPascalCase(serviceName)}";
        var baseUrl   = _configuration[configKey] ?? ServiceUrls.GetValueOrDefault(serviceName);
        if (baseUrl is null) return false;

        try
        {
            using var client = _httpFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            var response = await client.PostAsync($"{baseUrl}/{path}", null, ct);
            _logger.LogInformation("Action {Path} on {Service}: {Status}", path, serviceName, response.StatusCode);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Action {Path} on {Service} failed", path, serviceName);
            return false;
        }
    }

    private static string ToPascalCase(string s) =>
        string.Concat(s.Split('-').Select(w => char.ToUpper(w[0]) + w[1..]));
}
