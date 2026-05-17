using Messaging;
using Prometheus;
using StorageService.Services;
using StorageService.Workers;

var builder = WebApplication.CreateBuilder(args);

// ─── Kafka messaging ─────────────────────────────────────────────────────────
builder.Services.AddKafkaMessaging(builder.Configuration);

// ─── Storage services ─────────────────────────────────────────────────────────
builder.Services.AddSingleton<RedisDeduplicator>();
builder.Services.AddSingleton<ClickHouseWriter>();
builder.Services.AddSingleton<PostgresWriter>();

// ─── Health checks ───────────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

// ─── Background worker ───────────────────────────────────────────────────────
builder.Services.AddHostedService<StorageWorker>();

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapMetrics();

app.Run();
