using ApiGateway.Hubs;
using ApiGateway.Models;
using Confluent.Kafka;
using Contracts;
using Contracts.Events;
using Messaging;
using Microsoft.AspNetCore.SignalR;

namespace ApiGateway.Workers;

public class FeedBroadcastWorker : BackgroundService
{
    private readonly KafkaConsumerFactory _consumerFactory;
    private readonly IHubContext<FeedHub> _hub;
    private readonly ILogger<FeedBroadcastWorker> _logger;

    private const string GroupId = "api-gateway-feed-group";

    public FeedBroadcastWorker(
        KafkaConsumerFactory consumerFactory,
        IHubContext<FeedHub> hub,
        ILogger<FeedBroadcastWorker> logger)
    {
        _consumerFactory = consumerFactory;
        _hub             = hub;
        _logger          = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        using var consumer = _consumerFactory.Create(GroupId);
        consumer.Subscribe(KafkaTopics.AggregatedFeed);
        _logger.LogInformation("FeedBroadcastWorker subscribed to {Topic}", KafkaTopics.AggregatedFeed);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var result = consumer.Consume(TimeSpan.FromSeconds(1));
                if (result is null or { IsPartitionEOF: true })
                    continue;

                var evt = KafkaConsumerFactory.Deserialize<AggregatedMatchEvent>(result);
                consumer.Commit(result);

                var dto = MapToDto(evt);

                // Broadcast to all clients and to match-specific group
                await _hub.Clients.All.SendAsync("MatchUpdated", dto, stoppingToken);
                await _hub.Clients
                    .Group($"match:{evt.MatchId}")
                    .SendAsync("MatchUpdated", dto, stoppingToken);

                _logger.LogDebug("Broadcasted MatchUpdated for match {MatchId}", evt.MatchId);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        finally
        {
            consumer.Close();
        }
    }

    private static AggregatedMatchDto MapToDto(AggregatedMatchEvent evt) => new()
    {
        MatchId      = evt.MatchId,
        HomeTeam     = evt.HomeTeam,
        AwayTeam     = evt.AwayTeam,
        League       = evt.League,
        KickOff      = evt.KickOff,
        TotalTippers = evt.TotalTippers,
        Consensus    = evt.Consensus,
        Tips         = evt.Tips.Select(t => new TipDetailDto
        {
            ChannelId    = t.ChannelId,
            ChannelTitle = t.ChannelTitle,
            TipType      = t.TipType,
            TipValue     = t.TipValue,
            Odds         = t.Odds
        }).ToList(),
        UpdatedAt = evt.UpdatedAt
    };
}
