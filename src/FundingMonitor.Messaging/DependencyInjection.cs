using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FundingMonitor.Messaging;

public static class DependencyInjection
{
    public static IServiceCollection AddFundingMonitorMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Host), "Host is required")
            .Validate(options => options.Port is > 0 and <= 65535, "Port is invalid")
            .Validate(options => options.PublisherConfirmTimeoutSeconds > 0,
                "PublisherConfirmTimeoutSeconds must be positive")
            .ValidateOnStart();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<RabbitMqConnection>();
        services.AddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();

        return services;
    }
}