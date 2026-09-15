using CaroGame.Infrastructure.Networking.Messaging;
using CaroGame.Shared.Networking.Messaging;


namespace CaroGame.Server.Routing;

public class MessageDispatcher
{

    private readonly Dictionary<MessageTypes, RequestDelegate> _routes = new();

    public IReadOnlyCollection<MessageTypes> RegisteredMessageTypes => _routes.Keys;

    public MessageDispatcher(IEnumerable<KeyValuePair<MessageTypes, RequestDelegate>> routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        foreach (var (messageType, action) in routes)
        {
            ArgumentNullException.ThrowIfNull(action);

            if (!Enum.IsDefined(messageType))
                throw new ArgumentException(
                    $"Invalid route message type '{(byte)messageType}'.",
                    nameof(routes));

            if (!_routes.TryAdd(messageType, action))
                throw new InvalidOperationException(
                    $"More than one route is registered for message type '{messageType}'.");
        }
    }

    public Task DispatchAsync(
        ClientConnection connection,
        Packet packet,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(packet);
        ArgumentNullException.ThrowIfNull(packet.Payload);
        cancellationToken.ThrowIfCancellationRequested();

        if (!_routes.TryGetValue(packet.MessageType, out var action))
            throw new NotSupportedException(
                $"No route is registered for message type '{packet.MessageType}' ({(byte)packet.MessageType}).");

        return action(connection, packet, cancellationToken);
    }
}
