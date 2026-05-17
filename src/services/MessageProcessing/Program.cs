using Messaging;
using MessageProcessing.Services;
using MessageProcessing.Workers;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// ─── Kafka messaging ─────────────────────────────────────────────────────────
builder.Services.AddKafkaMessaging(builder.Configuration);

// ─── Team cache (singleton + hosted service for periodic refresh) ─────────────
builder.Services.AddSingleton<TeamCache>();
builder.Services.AddSingleton<ITeamCache>(sp => sp.GetRequiredService<TeamCache>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<TeamCache>());

// ─── Processing services ──────────────────────────────────────────────────────
builder.Services.AddSingleton<TeamRecognizer>();

// ─── Health checks ───────────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

// ─── Background worker ───────────────────────────────────────────────────────
builder.Services.AddHostedService<ProcessingWorker>();

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapMetrics();

app.Run();
