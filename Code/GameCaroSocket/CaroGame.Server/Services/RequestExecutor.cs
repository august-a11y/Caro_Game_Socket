using System.Text.Json;
using CaroGame.Shared.Networking.Messaging;
using CaroGame.Shared.Protocol.Contracts;

namespace CaroGame.Server.Services;

/// <summary>
/// Converts a packet payload into a typed request.
/// </summary>
public sealed class RequestExecutor
{
    private readonly IMessageSerializer _serializer;

    public RequestExecutor(IMessageSerializer serializer)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    public TRequest Deserialize<TRequest>(Packet packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        ArgumentNullException.ThrowIfNull(packet.Payload);

        if (packet.Payload.Length == 0 && typeof(TRequest) == typeof(RequestMessage))
            return (TRequest)(object)new RequestMessage();

        TRequest request;
        try
        {
            request = _serializer.Deserialize<TRequest>(packet.Payload);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                $"Payload for '{typeof(TRequest).Name}' is not valid JSON.",
                exception);
        }
        catch (NotSupportedException exception)
        {
            throw new InvalidDataException(
                $"Payload for '{typeof(TRequest).Name}' cannot be deserialized.",
                exception);
        }

        if (request is null)
            throw new InvalidDataException(
                $"Payload for '{typeof(TRequest).Name}' must not be null.");

        return request;
    }
}
