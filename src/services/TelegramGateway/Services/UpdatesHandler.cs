using Contracts.Events;
using Prometheus;
using TL;

namespace TelegramGateway.Services;

public class UpdatesHandler
{
    private readonly MessagePublisher _publisher;
    private readonly ILogger<UpdatesHandler> _logger;

    private IReadOnlyDictionary<long, string> _channels = new Dictionary<long, string>();

    private static readonly Counter Received = Metrics.CreateCounter(
        "telegram_messages_received_total",
        "Total channel messages received from monitored Telegram channels.");

    private static readonly Counter Skipped = Metrics.CreateCounter(
        "telegram_messages_skipped_total",
        "Messages dropped because the source channel is not monitored.");

    public UpdatesHandler(MessagePublisher publisher, ILogger<UpdatesHandler> logger)
    {
        _publisher = publisher;
        _logger    = logger;
    }

    public void SetChannels(IReadOnlyDictionary<long, string> channels) =>
        _channels = channels;

    public async Task HandleAsync(Update update)
    {
        if (update is not UpdateNewChannelMessage { message: Message msg })
            return;

        if (msg.peer_id is not PeerChannel { channel_id: var channelId })
            return;

        if (!_channels.TryGetValue(channelId, out var channelTitle))
        {
            Skipped.Inc();
            return;
        }

        Received.Inc();

        // Convert back to Bot API format (how the channel is stored in our DB)
        long botApiId = -(channelId + 1_000_000_000_000L);

        var evt = new RawMessageEvent
        {
            MessageId      = msg.id,
            ChatId         = botApiId,
            ChatTitle      = channelTitle,
            SenderId       = msg.from_id is PeerUser pu ? pu.user_id : 0,
            SenderUsername = string.Empty,
            MessageText    = msg.message,
            HasMedia       = msg.media is not null,
            MediaType      = msg.media?.GetType().Name ?? string.Empty,
            MediaFileId    = string.Empty,
            Timestamp      = new DateTimeOffset(msg.date, TimeSpan.Zero),
            Source         = "telegram",
            AccountId      = Guid.Empty
        };

        _logger.LogDebug("Message {MsgId} from channel {Title} ({ChannelId}): {Preview}",
            msg.id, channelTitle, channelId,
            msg.message.Length > 80 ? msg.message[..80] + "…" : msg.message);

        await _publisher.PublishAsync(evt);
    }
}
