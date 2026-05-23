namespace ApiGateway.Middleware;

public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string? _apiKey;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next   = next;
        _apiKey = configuration["ApiKey"];
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip auth for health, metrics, SignalR hub, and Swagger
        if (ShouldSkip(context.Request.Path))
        {
            await _next(context);
            return;
        }

        if (string.IsNullOrEmpty(_apiKey))
        {
            // No API key configured → open access (dev mode)
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("X-Api-Key", out var key) ||
            key != _apiKey)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }

        await _next(context);
    }

    private static bool ShouldSkip(PathString path) =>
        path.StartsWithSegments("/health") ||
        path.StartsWithSegments("/metrics") ||
        path.StartsWithSegments("/hubs") ||
        path.StartsWithSegments("/swagger") ||
        path.StartsWithSegments("/admin");
}
