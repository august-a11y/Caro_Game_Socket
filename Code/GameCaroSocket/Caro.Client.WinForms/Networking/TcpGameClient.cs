using System.Net.Sockets;
using CaroGame.Shared.Networking.Messaging;

namespace Caro.Client.WinForms.Networking;

using Caro.Client.WinForms.Core;

internal sealed class TcpGameClient : IAsyncDisposable
{
    private readonly ClientLogger _logger = ClientLogger.Shared;
    private readonly PacketFramer _framer;
    private readonly JsonMessageSerializer _serializer;
    private readonly SemaphoreSlim _sendGate = new(1, 1);
    private readonly SemaphoreSlim _connectGate = new(1, 1);
    private readonly object _sync = new();
    private TcpClient? _client;
    private NetworkStream? _stream;
    private CancellationTokenSource? _receiveCancellation;
    private Task? _receiveLoop;
    private bool _disposed;

    public bool IsConnected => _stream is not null && _client?.Connected == true;
    public event Action? Connected;
    public event Action? Disconnected;
    public event Action<Exception>? TransportError;
    public event Func<Packet, CancellationToken, Task>? PacketReceived;

    public TcpGameClient(PacketFramer framer, JsonMessageSerializer serializer)
    {
        _framer = framer ?? throw new ArgumentNullException(nameof(framer));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        _logger.Info($"TCP connect requested: {host}:{port}");
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(port);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(port, 65535);
        await _connectGate.WaitAsync(cancellationToken);
        try
        {
            lock (_sync)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                if (IsConnected)
                    throw new InvalidOperationException("The client is already connected.");
            }

            await DisconnectAsync();
            var client = new TcpClient();
            try
            {
                await client.ConnectAsync(host, port, cancellationToken);
                var stream = client.GetStream();
                var receiveCancellation = new CancellationTokenSource();
                lock (_sync)
                {
                    ObjectDisposedException.ThrowIf(_disposed, this);
                    _client = client;
                    _stream = stream;
                    _receiveCancellation = receiveCancellation;
                    _receiveLoop = Task.Run(() => ReceiveLoopAsync(stream, receiveCancellation.Token));
                }
                Connected?.Invoke();
                _logger.Info($"TCP connected: {host}:{port}");
            }
            catch (Exception exception)
            {
                _logger.Warning($"TCP connect failed: {host}:{port}", exception);
                client.Dispose();
                throw;
            }
        }
        finally { _connectGate.Release(); }
    }

    public async Task SendAsync<TPayload>(MessageTypes messageType, TPayload payload,
        CancellationToken cancellationToken = default)
    {
        NetworkStream stream;
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            stream = _stream ?? throw new InvalidOperationException("The client is not connected.");
        }

        var packet = new Packet(_serializer.Serialize(payload), messageType);
        _logger.Debug($"TCP send: {messageType}, {packet.Payload.Length} bytes");
        await _sendGate.WaitAsync(cancellationToken);
        try { await _framer.WriteAsync(stream, packet, cancellationToken); }
        finally { _sendGate.Release(); }
    }

    public async Task DisconnectAsync()
    {
        _logger.Info("TCP disconnect requested.");
        Task? receiveLoop;
        lock (_sync)
        {
            receiveLoop = _receiveLoop;
            _receiveCancellation?.Cancel();
            _stream?.Dispose();
            _client?.Dispose();
        }

        if (receiveLoop is not null && receiveLoop.Id != Task.CurrentId)
        {
            try { await receiveLoop; }
            catch (OperationCanceledException) { }
        }
    }

    private async Task ReceiveLoopAsync(Stream stream, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var packet = await _framer.ReadAsync(stream, cancellationToken);
                if (packet is null)
                    break;
                _logger.Debug($"TCP receive: {packet.MessageType}, {packet.Payload.Length} bytes");
                var handlers = PacketReceived?.GetInvocationList()
                    .Cast<Func<Packet, CancellationToken, Task>>()
                    .ToArray() ?? [];
                foreach (var handler in handlers)
                    await handler(packet, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception exception)
        {
            _logger.Error("TCP receive loop failed.", exception);
            TransportError?.Invoke(exception);
        }
        finally
        {
            lock (_sync)
            {
                _stream?.Dispose();
                _client?.Dispose();
                _stream = null;
                _client = null;
                _receiveCancellation?.Dispose();
                _receiveCancellation = null;
            }
            _logger.Info("TCP disconnected.");
            Disconnected?.Invoke();
        }
    }

    public async ValueTask DisposeAsync()
    {
        lock (_sync) _disposed = true;
        await DisconnectAsync();
        _sendGate.Dispose();
    }
}
