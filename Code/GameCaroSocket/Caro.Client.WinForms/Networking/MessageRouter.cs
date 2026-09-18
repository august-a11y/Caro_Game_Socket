using CaroGame.Shared.Networking.Messaging;

namespace Caro.Client.WinForms.Networking;

using Caro.Client.WinForms.Core;

internal sealed class MessageRouter
{
    private readonly Dictionary<MessageTypes, Func<Packet, CancellationToken, Task>> _handlers = new();
    private readonly JsonMessageSerializer _serializer;
    private readonly ClientLogger _logger = ClientLogger.Shared;

    public event Action<Packet>? UnhandledMessage;
    public event Action<MessageTypes, Exception>? HandlerFailed;

    public MessageRouter(JsonMessageSerializer serializer)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    public void Register(MessageTypes messageType, Func<Packet, CancellationToken, Task> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        if (!_handlers.TryAdd(messageType, handler))
            throw new InvalidOperationException($"A handler is already registered for '{messageType}'.");
    }

    public void Register<TPayload>(MessageTypes messageType, Func<TPayload, CancellationToken, Task> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        Register(messageType, async (packet, cancellationToken) =>
        {
            var payload = _serializer.Deserialize<TPayload>(packet.Payload)
                ?? throw new InvalidDataException($"Payload for '{messageType}' must not be null.");
            await handler(payload, cancellationToken);
        });
    }

    public async Task RouteAsync(Packet packet, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(packet);
        if (!_handlers.TryGetValue(packet.MessageType, out var handler))
        {
            _logger.Warning($"Unhandled packet: {packet.MessageType}, {packet.Payload.Length} bytes");
            UnhandledMessage?.Invoke(packet);
            return;
        }
        try
        {
            await handler(packet, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.Error($"Packet handler failed: {packet.MessageType}", exception);
            HandlerFailed?.Invoke(packet.MessageType, exception);
        }
    }
}
