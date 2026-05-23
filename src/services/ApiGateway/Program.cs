using ApiGateway.Hubs;
using ApiGateway.Middleware;
using ApiGateway.Services;
using ApiGateway.Workers;
using Messaging;
using Prometheus;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// ─── Controllers + Swagger ────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Stoixima API", Version = "v1" });
    c.AddSecurityDefinition("ApiKey", new()
    {
        In   = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = "X-Api-Key",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey
    });
});

// ─── SignalR ──────────────────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ─── CORS — allow frontend dev server ────────────────────────────────────────
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:5173", "http://localhost:3000")
     .AllowAnyHeader()
     .AllowAnyMethod()
     .AllowCredentials()));

// ─── Kafka messaging ─────────────────────────────────────────────────────────
builder.Services.AddKafkaMessaging(builder.Configuration);

// ─── Redis ────────────────────────────────────────────────────────────────────
var redisCs = builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException("Missing Redis connection string.");
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisCs));

// ─── HTTP client (for health checks + action forwarding) ─────────────────────
builder.Services.AddHttpClient();

// ─── Services ─────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<FeedRepository>();
builder.Services.AddSingleton<AdminRepository>();
builder.Services.AddSingleton<ServiceHealthChecker>();
builder.Services.AddSingleton<ServiceActionClient>();

// ─── Background workers ───────────────────────────────────────────────────────
builder.Services.AddHostedService<FeedBroadcastWorker>();

// ─── Health checks ────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

var app = builder.Build();

// ─── Pipeline ─────────────────────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Stoixima API v1"));

app.UseCors();
app.UseMiddleware<ApiKeyMiddleware>();

app.MapControllers();
app.MapHub<FeedHub>("/hubs/feed");
app.MapHealthChecks("/health");
app.MapMetrics();

app.Run();
