using CaroGame.Server.Services;
using System.Net.Sockets;
using System.Text.Json;
using System.Diagnostics;
using CaroGame.Server.Controllers;
using CaroGame.Server.Routing;
using CaroGame.Shared.Networking.Messaging;
using CaroGame.Shared.Protocol.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public class ClientConnectionHandler(
        MessageDispatcher dispatcher,
        SessionController sessions,
        MessageService messages,
        ILogger<ClientConnectionHandler>? logger = null)
{
    private readonly ILogger<ClientConnectionHandler> _logger = logger ?? NullLogger<ClientConnectionHandler>.Instance;

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
                var startedAt = Stopwatch.GetTimestamp();
                try
                {
                    if (packet.MessageType is not MessageTypes.PlayerJoinRequest and not MessageTypes.PlayerReconnectRequest)
                        connection.RequireSession();
                    await dispatcher.DispatchAsync(connection, packet, cancellationToken);
                    _logger.LogInformation(
                        "Request handled | ConnectionId={ConnectionId} PlayerId={PlayerId} " +
                        "MessageType={MessageType} RequestId={RequestId} ElapsedMs={ElapsedMs:F2}",
                        connection.ConnectionId,
                        connection.Session?.PlayerId,
                        packet.MessageType,
                        RequestId(packet),
                        Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
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
                    _logger.LogWarning(
                        "Request rejected | ConnectionId={ConnectionId} PlayerId={PlayerId} " +
                        "MessageType={MessageType} RequestId={RequestId} ErrorCode={ErrorCode} " +
                        "Reason={Reason} ElapsedMs={ElapsedMs:F2}",
                        connection.ConnectionId,
                        connection.Session?.PlayerId,
                        packet.MessageType,
                        RequestId(packet),
                        code,
                        exception.Message,
                        Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
                    var type = packet.MessageType == MessageTypes.MoveRequest
                        ? MessageTypes.MoveRejected : MessageTypes.ErrorResponse;
                    await messages.SendResponseAsync(connection, type, response, cancellationToken);
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Request failed | ConnectionId={ConnectionId} PlayerId={PlayerId} " +
                        "MessageType={MessageType} RequestId={RequestId} ElapsedMs={ElapsedMs:F2}",
                        connection.ConnectionId,
                        connection.Session?.PlayerId,
                        packet.MessageType,
                        RequestId(packet),
                        Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
                    throw;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (exception is SocketException or IOException or ObjectDisposedException or OperationCanceledException)
        {
            _logger.LogInformation(exception, "Connection {ConnectionId} closed", connection.ConnectionId);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected connection error for {ConnectionId}",
                connection.ConnectionId);
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
