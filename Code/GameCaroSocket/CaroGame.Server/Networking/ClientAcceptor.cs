using System.Net;
using System.Collections.Concurrent;
using System.Net.Sockets;
using CaroGame.Infrastructure.Networking.Messaging;

public class ClientAcceptor : IAsyncDisposable
{
    private readonly Socket _listener;
    private readonly IPacketFramer _packetFramer;
    private readonly ClientConnectionHandler _connectionHandler;
    private readonly ConcurrentDictionary<Guid, ClientConnection> _connections;
    private readonly ConcurrentDictionary<Guid, Task> _handlers = new();
    private readonly CancellationTokenSource _shutdown = new();
    private Task? _run;

    public IPEndPoint LocalEndPoint => (IPEndPoint)_listener.LocalEndPoint!;

    internal ClientConnection[] Connections => _connections.Values.ToArray();

    public ClientAcceptor(
        IPEndPoint endPoint,
        IPacketFramer packetFramer,
        ClientConnectionHandler connectionHandler,
        ConcurrentDictionary<Guid, ClientConnection> connections)
    {
        _packetFramer = packetFramer;
        _connectionHandler = connectionHandler;
        _connections = connections;

        _listener = new Socket(
            endPoint.AddressFamily,
            SocketType.Stream,
            ProtocolType.Tcp);

        _listener.Bind(endPoint);
        _listener.Listen(backlog: 100);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_run is not null)
            throw new InvalidOperationException("The listener has already started.");
        return _run = AcceptLoopAsync(cancellationToken);
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _shutdown.Token);
        cancellationToken = linked.Token;
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Socket clientSocket =
                    await _listener.AcceptAsync(
                        cancellationToken);

                var connection = new ClientConnection(
                    clientSocket,
                    _packetFramer,
                    _connections);
                _connections.TryAdd(connection.ConnectionId, connection);

                // Mỗi client có lifetime xử lý riêng.
                var task = HandleConnectionAsync(connection, cancellationToken);
                _handlers.TryAdd(connection.ConnectionId, task);
                _ = task.ContinueWith(completed =>
                {
                    _handlers.TryRemove(connection.ConnectionId, out _);
                }, CancellationToken.None);
            }
        }
        catch (OperationCanceledException)
        {
            // Server đang dừng.
        }
        catch (ObjectDisposedException)
        {
            // Listener đã bị đóng.
        }
    }

    private async Task HandleConnectionAsync(
        ClientConnection connection,
        CancellationToken cancellationToken)
    {
        try
        {
            await _connectionHandler.HandleClientConnectionAsync(
                connection,
                cancellationToken);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
        finally
        {
            _connections.TryRemove(connection.ConnectionId, out _);
            await connection.DisposeAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _shutdown.Cancel();
        _listener.Dispose();
        if (_run is not null)
            await _run;
        await Task.WhenAll(_handlers.Values.ToArray());
        _shutdown.Dispose();
    }

}

