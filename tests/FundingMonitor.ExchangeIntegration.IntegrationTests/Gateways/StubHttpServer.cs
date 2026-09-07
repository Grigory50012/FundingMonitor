using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FundingMonitor.ExchangeIntegration.IntegrationTests.Gateways;

internal sealed class StubHttpServer : IAsyncDisposable
{
    private readonly CancellationTokenSource _stopping = new();
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly ConcurrentDictionary<string, StubResponse> _responses = new(StringComparer.Ordinal);
    private readonly ConcurrentBag<string> _requests = [];
    private Task? _acceptLoop;

    internal Uri BaseAddress { get; private set; } = null!;

    internal IReadOnlyCollection<string> Requests => _requests;

    internal void Start()
    {
        _listener.Start();
        var endpoint = (IPEndPoint)_listener.LocalEndpoint;
        BaseAddress = new Uri($"http://127.0.0.1:{endpoint.Port}/");
        _acceptLoop = AcceptLoopAsync(_stopping.Token);
    }

    internal void Respond(string path, string json, HttpStatusCode statusCode = HttpStatusCode.OK) =>
        _responses[path] = new StubResponse(statusCode, json);

    internal bool ReceivedPath(string path) =>
        _requests.Any(request => request.StartsWith(path, StringComparison.Ordinal));

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            _ = HandleAsync(client, cancellationToken);
        }
    }

    private async Task HandleAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using (client)
        await using (var stream = client.GetStream())
        using (var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true))
        {
            var requestLine = await reader.ReadLineAsync(cancellationToken) ?? string.Empty;
            var target = requestLine.Split(' ', StringSplitOptions.RemoveEmptyEntries).ElementAtOrDefault(1)
                         ?? "/";
            _requests.Add(target);

            while (!string.IsNullOrEmpty(await reader.ReadLineAsync(cancellationToken)))
            {
            }

            var path = target.Split('?', 2)[0];
            var response = _responses.TryGetValue(path, out var configured)
                ? configured
                : new StubResponse(HttpStatusCode.NotFound, "{}");
            var body = Encoding.UTF8.GetBytes(response.Json);
            var headers = Encoding.ASCII.GetBytes(
                $"HTTP/1.1 {(int)response.StatusCode} {response.StatusCode}\r\n" +
                "Content-Type: application/json\r\n" +
                $"Content-Length: {body.Length}\r\n" +
                "Connection: close\r\n\r\n");

            await stream.WriteAsync(headers, cancellationToken);
            await stream.WriteAsync(body, cancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _stopping.CancelAsync();
        _listener.Stop();
        if (_acceptLoop is not null)
        {
            try
            {
                await _acceptLoop;
            }
            catch (SocketException) when (_stopping.IsCancellationRequested)
            {
            }
        }

        _stopping.Dispose();
    }

    private sealed record StubResponse(HttpStatusCode StatusCode, string Json);
}
