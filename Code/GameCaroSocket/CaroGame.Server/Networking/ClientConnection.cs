using System.Net;
using System.Net.Sockets;
using CaroGame.Domain.Entities;
using CaroGame.Shared.Networking.Messaging;

public sealed class ClientConnection : IAsyncDisposable
{

    private readonly Socket _socket;
    private readonly IPacketFramer _framer;
    private readonly SemaphoreSlim _sendGate = new(1, 1);
    private readonly IReadOnlyDictionary<Guid, ClientConnection> _connections;

    public Guid ConnectionId { get; } = Guid.NewGuid();

    public EndPoint RemoteEndPoint =>
        _socket.RemoteEndPoint!;

    public Session? Session { get; private set; }

    public bool IsAuthenticated =>
        Session is { IsConnected: true };

    public Session RequireSession() => Session is { IsConnected: true } session
        ? session
        : throw new UnauthorizedAccessException("Join or reconnect before sending this request.");

    public ClientConnection(
        Socket socket,
        IPacketFramer framer,
        IReadOnlyDictionary<Guid, ClientConnection> connections)
    {
        _socket = socket;

        _framer = framer;
        _connections = connections;
    }

    public Task<Packet?> ReceiveAsync(
        CancellationToken cancellationToken = default)
    {
        return _framer.ReadPacketAsync(_socket, cancellationToken);
    }

    public Task SendAsync(
        Packet packet,
        CancellationToken cancellationToken = default)
        => SendPacketsAsync([packet], cancellationToken);

    internal async Task SendPacketsAsync(
        IReadOnlyList<Packet> packets, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        await _sendGate.WaitAsync(timeout.Token);
        try
        {
            foreach (var packet in packets)
                await _framer.WriteAsync(_socket, packet, timeout.Token);
        }
        catch
        {
            // A failed or cancelled write may leave a partial frame on the stream.
            await DisposeAsync();
            throw;
        }
        finally
        {
            _sendGate.Release();
        }
    }

    public void AttachSession(Session session, bool replaceExisting = false)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (Session is not null)
            throw new InvalidOperationException("This connection already has a session.");
        if (replaceExisting)
        {
            foreach (var previous in _connections.Values)
            {
                if (previous != this && previous.Session?.SessionId == session.SessionId)
                {
                    // Detach first so old-connection cleanup cannot disconnect the replacement.
                    previous.Session = null;
                    previous._socket.Dispose();
                }
            }
        }
        Session = session;
    }

    public ValueTask DisposeAsync()
    {
        _socket.Dispose();
        return ValueTask.CompletedTask;
    }
}
