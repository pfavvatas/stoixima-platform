using Microsoft.AspNetCore.SignalR;
using Prometheus;

namespace ApiGateway.Hubs;

public class FeedHub : Hub
{
    private static readonly Gauge ActiveConnections = Metrics.CreateGauge(
        "websocket_connections_active", "Active SignalR WebSocket connections");

    public override Task OnConnectedAsync()
    {
        ActiveConnections.Inc();
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        ActiveConnections.Dec();
        return base.OnDisconnectedAsync(exception);
    }

    // Client calls this to subscribe to updates for a specific match.
    public async Task SubscribeToMatch(int matchId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"match:{matchId}");
    }

    public async Task UnsubscribeFromMatch(int matchId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"match:{matchId}");
    }
}
