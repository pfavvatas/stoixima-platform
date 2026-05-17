using Messaging;
using Prometheus;
using TelegramGateway.Configuration;
using TelegramGateway.Health;
using TelegramGateway.Services;
using TelegramGateway.Workers;

var builder = WebApplication.CreateBuilder(args);

// ─── Configuration ────────────────────────────────────────────────────────────
builder.Services.Configure<TelegramOptions>(
    builder.Configuration.GetSection(TelegramOptions.Section));

// ─── Kafka messaging ─────────────────────────────────────────────────────────
builder.Services.AddKafkaMessaging(builder.Configuration);

// ─── Telegram services ───────────────────────────────────────────────────────
builder.Services.AddSingleton<ChannelRepository>();
builder.Services.AddSingleton<MessagePublisher>();
builder.Services.AddSingleton<UpdatesHandler>();

// ─── Health checks ───────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddCheck<TelegramConnectionHealthCheck>("telegram_connection");

// ─── Background worker ───────────────────────────────────────────────────────
builder.Services.AddHostedService<TelegramWorker>();

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapMetrics();

app.Run();
