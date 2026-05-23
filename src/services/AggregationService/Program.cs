using AggregationService.Services;
using AggregationService.Workers;
using Messaging;
using Prometheus;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// ─── Controllers ──────────────────────────────────────────────────────────────
builder.Services.AddControllers();

// ─── Kafka messaging ─────────────────────────────────────────────────────────
builder.Services.AddKafkaMessaging(builder.Configuration);

// ─── Redis ────────────────────────────────────────────────────────────────────
var redisCs = builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException("Missing Redis connection string.");
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisCs));

// ─── Services ─────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<MatchAggregator>();

// ─── Background worker ────────────────────────────────────────────────────────
builder.Services.AddHostedService<AggregationWorker>();

// ─── Health + metrics ─────────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapMetrics();

app.Run();
