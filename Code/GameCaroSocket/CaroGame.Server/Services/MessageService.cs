using System.Collections.Concurrent;
using System.Net.Sockets;
using CaroGame.Infrastructure.Networking.Messaging;
using CaroGame.Shared.Networking.Messaging;

namespace CaroGame.Server.Services;

public sealed class MessageService(
    IMessageSerializer serializer,
    ConcurrentDictionary<Guid, ClientConnection> connections)
{
    public Packet CreatePacket<T>(MessageTypes type, T payload)
        => new(serializer.Serialize(payload), type);

    public Task SendResponseAsync<T>(ClientConnection connection, MessageTypes type, T response,
        CancellationToken cancellationToken = default)
        => connection.SendAsync(CreatePacket(type, response), cancellationToken);

    public Task BroadcastAsync<T>(IEnumerable<Guid> playerIds, MessageTypes type, T notification,
        CancellationToken cancellationToken = default)
        => BroadcastAsync(playerIds, CreatePacket(type, notification), cancellationToken);

    public Task BroadcastAsync(IEnumerable<Guid> playerIds, Packet packet,
        CancellationToken cancellationToken = default)
        => BroadcastAsync(playerIds, new[] { packet }, cancellationToken);

    public Task BroadcastAsync(IEnumerable<Guid> playerIds, IReadOnlyList<Packet> packets,
        CancellationToken cancellationToken = default)
    {
        var recipients = playerIds.ToHashSet();
        // Start sends immediately so callers can enqueue under their state lock,
        // then await delivery after releasing it. Each connection preserves batch order.
        var sends = connections.Values
            .Where(connection => connection.Session is { IsConnected: true } session &&
                recipients.Contains(session.PlayerId))
            .Select(connection => SendToClientAsync(connection, packets, cancellationToken))
            .ToArray();
        return Task.WhenAll(sends);
    }

    private static async Task SendToClientAsync(ClientConnection connection,
        IReadOnlyList<Packet> packets, CancellationToken cancellationToken)
    {
        try
        {
            await connection.SendPacketsAsync(packets, cancellationToken);
        }
        catch (Exception exception) when (exception is SocketException or IOException or
            ObjectDisposedException or OperationCanceledException)
        {
            Console.WriteLine($"Broadcast to {connection.ConnectionId} failed: {exception.Message}");
            await connection.DisposeAsync();
        }
    }
}
