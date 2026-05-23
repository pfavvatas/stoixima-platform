using MatchDataService.Configuration;
using MatchDataService.Services;
using MatchDataService.Workers;
using Polly;
using Microsoft.Extensions.Http;
using Prometheus;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// ─── Configuration ────────────────────────────────────────────────────────────
builder.Services.Configure<FootballApiOptions>(
    builder.Configuration.GetSection(FootballApiOptions.Section));

var footballOptions = builder.Configuration
    .GetSection(FootballApiOptions.Section)
    .Get<FootballApiOptions>() ?? new();

// ─── HTTP client with retry policy ───────────────────────────────────────────
builder.Services.AddHttpClient<FootballApiClient>(client =>
{
    client.BaseAddress = new Uri(footballOptions.BaseUrl);
    client.DefaultRequestHeaders.Add("X-Auth-Token", footballOptions.ApiKey);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddPolicyHandler(Policy<HttpResponseMessage>
    .Handle<HttpRequestException>()
    .OrResult(r => !r.IsSuccessStatusCode)
    .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))));

// ─── Redis ────────────────────────────────────────────────────────────────────
var redisCs = builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException("Missing Redis connection string.");
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisCs));

// ─── Services ─────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<TeamNameNormalizer>();
builder.Services.AddSingleton<MatchRepository>();

// ─── Background worker ────────────────────────────────────────────────────────
builder.Services.AddHostedService<MatchFetchWorker>();

// ─── Health + metrics ─────────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapMetrics();

app.Run();
