using FundingMonitor.Core.Configuration;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace FundingMonitor.Infrastructure.Messaging.RabbitMq;

public sealed class RabbitMqConnection : IAsyncDisposable
{
    private readonly ConnectionFactory _factory;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private IConnection? _connection;

    public RabbitMqConnection(IOptions<RabbitMqOptions> options)
    {
        var value = options.Value;
        _factory = new ConnectionFactory
        {
            HostName = value.Host,
            Port = value.Port,
            VirtualHost = value.VirtualHost,
            UserName = value.Username,
            Password = value.Password,
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true,
            ClientProvidedName = "funding-monitor"
        };
    }

    public async Task<IConnection> GetAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true })
            return _connection;

        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_connection is not { IsOpen: true })
            {
                if (_connection is not null)
                    await _connection.DisposeAsync();

                _connection = await _factory.CreateConnectionAsync(cancellationToken);
            }

            return _connection;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();

        _connectionLock.Dispose();
    }
}
