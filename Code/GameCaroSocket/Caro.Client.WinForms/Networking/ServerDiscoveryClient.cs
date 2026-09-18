using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using CaroGame.Shared.Networking;

namespace Caro.Client.WinForms.Networking;

public sealed class ServerDiscoveryClient : IAsyncDisposable
{
    private readonly JsonSerializerOptions _jsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

    private readonly object _sync = new();

    private UdpClient? _udpClient;
    private CancellationTokenSource? _stopSource;
    private Task? _listenTask;
    private bool _disposed;

    public event Action<ServerInfo>? ServerDiscovered;
    public event Action<Exception>? DiscoveryError;

    public bool IsListening
    {
        get
        {
            lock (_sync)
            {
                return _listenTask is { IsCompleted: false };
            }
        }
    }

    public void Start()
    {
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_listenTask is { IsCompleted: false })
                return;

            var udpClient = new UdpClient(AddressFamily.InterNetwork);
            try
            {
                // Multiple clients on the same PC must all receive LAN broadcasts.
                udpClient.ExclusiveAddressUse = false;
                udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, ServerInfo.DiscoveryPort));
            }
            catch
            {
                udpClient.Dispose();
                throw;
            }

            var stopSource = new CancellationTokenSource();

            _udpClient = udpClient;
            _stopSource = stopSource;
            _listenTask = ListenAsync(
                udpClient,
                stopSource.Token);
        }
    }

    private async Task ListenAsync(
        UdpClient udpClient,
        CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await udpClient.ReceiveAsync(
                    cancellationToken).ConfigureAwait(false);

                var info = Deserialize(result.Buffer);

                if (info is null)
                    continue;

                try
                {
                    ServerDiscovered?.Invoke(info);
                }
                catch (Exception exception)
                {
                    RaiseDiscoveryError(exception);
                }
            }
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (ObjectDisposedException)
            when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            RaiseDiscoveryError(exception);
        }
    }

    private ServerInfo? Deserialize(byte[] bytes)
    {
        try
        {
            var info = JsonSerializer.Deserialize<ServerInfo>(
                bytes,
                _jsonOptions);

            if (info is null ||
                !string.Equals(
                    info.Service,
                    ServerInfo.ServiceName,
                    StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(info.Host) ||
                info.TcpPort is < 1 or > 65535)
            {
                return null;
            }

            return info;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public async Task StopAsync()
    {
        Task? listenTask;
        CancellationTokenSource? stopSource;
        UdpClient? udpClient;

        lock (_sync)
        {
            listenTask = _listenTask;
            stopSource = _stopSource;
            udpClient = _udpClient;

            _listenTask = null;
            _stopSource = null;
            _udpClient = null;

            stopSource?.Cancel();
            udpClient?.Dispose();
        }

        if (listenTask is not null)
        {
            try
            {
                await listenTask;
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }

        stopSource?.Dispose();
    }

    private void RaiseDiscoveryError(Exception exception)
    {
        try
        {
            DiscoveryError?.Invoke(exception);
        }
        catch
        {
            // Không để event handler làm dừng listener.
        }
    }

    public async ValueTask DisposeAsync()
    {
        lock (_sync)
        {
            if (_disposed)
                return;

            _disposed = true;
        }

        await StopAsync();
    }
}
