using Contracts.Events;
using StackExchange.Redis;

namespace StorageService.Services;

public class RedisDeduplicator : IAsyncDisposable
{
    private readonly ConnectionMultiplexer _redis;
    private readonly IDatabase _db;

    public RedisDeduplicator(IConfiguration configuration)
    {
        var cs = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Missing Redis connection string.");
        _redis = ConnectionMultiplexer.Connect(cs);
        _db    = _redis.GetDatabase();
    }

    // Checks each event against Redis using a pipeline (single round-trip).
    // Returns only events that are NEW (not seen before) and marks them as seen with 24 h TTL.
    public async Task<IReadOnlyList<RawMessageEvent>> FilterDuplicatesAsync(
        IReadOnlyList<RawMessageEvent> events, CancellationToken ct = default)
    {
        if (events.Count == 0)
            return Array.Empty<RawMessageEvent>();

        var batch = _db.CreateBatch();
        var tasks = events
            .Select(e => (
                Event: e,
                Task: batch.StringSetAsync(
                    $"msg:dedup:{e.ChatId}:{e.MessageId}",
                    "1",
                    TimeSpan.FromHours(24),
                    When.NotExists)))
            .ToList();

        batch.Execute();
        var results = await Task.WhenAll(tasks.Select(t => t.Task));

        return tasks
            .Where((t, i) => results[i])   // true  → key didn't exist → new message
            .Select(t => t.Event)
            .ToList();
    }

    public async ValueTask DisposeAsync()
    {
        await _redis.CloseAsync();
        _redis.Dispose();
    }
}
