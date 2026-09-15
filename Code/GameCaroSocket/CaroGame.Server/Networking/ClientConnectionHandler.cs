using CaroGame.Server.Services;
using System.Net.Sockets;
using System.Text.Json;
using CaroGame.Infrastructure.Networking.Messaging;
using CaroGame.Server.Controllers;
using CaroGame.Server.Routing;
using CaroGame.Shared.Networking.Messaging;
using CaroGame.Shared.Protocol.Contracts;

public class ClientConnectionHandler(
    MessageDispatcher dispatcher,
    SessionController sessions,
    MessageService messages)
{
    public async Task HandleClientConnectionAsync(ClientConnection connection, CancellationToken cancellationToken = default)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // A malformed TCP frame ends the connection; an invalid request does not.
                var packet = await connection.ReceiveAsync(cancellationToken);
                if (packet is null)
                    break;
                try
                {
                    if (packet.MessageType is not MessageTypes.PlayerJoinRequest and not MessageTypes.PlayerReconnectRequest)
                        connection.RequireSession();
                    await dispatcher.DispatchAsync(connection, packet, cancellationToken);
                }
                catch (Exception exception) when (exception is InvalidDataException or JsonException or
                    UnauthorizedAccessException or KeyNotFoundException or ArgumentException or
                    InvalidOperationException or NotSupportedException)
                {
                    var code = exception switch
                    {
                        InvalidDataException or JsonException => "InvalidPayload",
                        UnauthorizedAccessException => "Unauthorized",
                        KeyNotFoundException => "NotFound",
                        InvalidOperationException => "InvalidState",
                        _ => "InvalidRequest"
                    };
                    var response = new ErrorResponse(RequestId(packet), code, exception.Message);
                    var type = packet.MessageType == MessageTypes.MoveRequest
                        ? MessageTypes.MoveRejected : MessageTypes.ErrorResponse;
                    await messages.SendResponseAsync(connection, type, response, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (exception is SocketException or IOException or ObjectDisposedException or OperationCanceledException)
        {
            Console.WriteLine($"Connection {connection.ConnectionId} closed: {exception.Message}");
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Unexpected connection error: {exception}");
        }
        finally
        {
            try
            {
                await sessions.DisconnectAsync(connection);
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }
    }

    private static Guid? RequestId(Packet packet)
    {
        try
        {
            using var json = JsonDocument.Parse(packet.Payload);
            if (json.RootElement.ValueKind == JsonValueKind.Object &&
                json.RootElement.TryGetProperty("RequestId", out var value) &&
                value.ValueKind == JsonValueKind.String && value.TryGetGuid(out var id))
                return id;
        }
        catch (JsonException)
        {
        }
        return null;
    }
}
