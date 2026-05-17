using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Messaging;

public static class MessagingExtensions
{
    public static IServiceCollection AddKafkaMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.Section));
        services.AddSingleton<IEventPublisher, KafkaEventPublisher>();
        services.AddSingleton<KafkaConsumerFactory>();
        return services;
    }
}
