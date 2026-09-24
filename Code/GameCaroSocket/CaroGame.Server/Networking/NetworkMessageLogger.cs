using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using CaroGame.Shared.Networking.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CaroGame.Server.Networking;

/// <summary>
/// Writes a structured audit trail for every packet entering and leaving the server.
/// </summary>
public sealed class NetworkMessageLogger(ILogger<NetworkMessageLogger>? logger = null)
{
    private const int MaxPayloadLogLength = 16 * 1024;
    private const string RedactedValue = "***REDACTED***";
    private readonly ILogger<NetworkMessageLogger> _logger =
        logger ?? NullLogger<NetworkMessageLogger>.Instance;

    public void LogReceived(ClientConnection connection, Packet packet)
    {
        var payload = InspectPayload(packet.Payload);

        _logger.LogInformation(
            "TCP request received | ConnectionId={ConnectionId} RemoteEndPoint={RemoteEndPoint} " +
            "PlayerId={PlayerId} MessageType={MessageType} MessageTypeCode={MessageTypeCode} " +
            "RequestId={RequestId} PayloadBytes={PayloadBytes} Payload={Payload}",
            connection.ConnectionId,
            connection.RemoteEndPoint,
            connection.Session?.PlayerId,
            packet.MessageType,
            (byte)packet.MessageType,
            payload.RequestId,
            packet.Length,
            payload.Text);
    }

    public void LogSent(ClientConnection connection, Packet packet)
    {
        var payload = InspectPayload(packet.Payload);

        _logger.LogInformation(
            "TCP outbound message sent | ConnectionId={ConnectionId} RemoteEndPoint={RemoteEndPoint} " +
            "PlayerId={PlayerId} MessageType={MessageType} MessageTypeCode={MessageTypeCode} " +
            "RequestId={RequestId} PayloadBytes={PayloadBytes} Payload={Payload}",
            connection.ConnectionId,
            connection.RemoteEndPoint,
            connection.Session?.PlayerId,
            packet.MessageType,
            (byte)packet.MessageType,
            payload.RequestId,
            packet.Length,
            payload.Text);
    }

    public void LogSendFailed(ClientConnection connection, Packet packet, Exception exception)
    {
        var payload = InspectPayload(packet.Payload);

        _logger.LogWarning(
            exception,
            "TCP outbound message failed | ConnectionId={ConnectionId} RemoteEndPoint={RemoteEndPoint} " +
            "PlayerId={PlayerId} MessageType={MessageType} MessageTypeCode={MessageTypeCode} " +
            "RequestId={RequestId} PayloadBytes={PayloadBytes} Payload={Payload}",
            connection.ConnectionId,
            SafeRemoteEndPoint(connection),
            connection.Session?.PlayerId,
            packet.MessageType,
            (byte)packet.MessageType,
            payload.RequestId,
            packet.Length,
            payload.Text);
    }

    private static PayloadDetails InspectPayload(byte[] payload)
    {
        if (payload.Length == 0)
            return new PayloadDetails(null, "<empty>");

        try
        {
            using var document = JsonDocument.Parse(payload);
            var requestId = FindRequestId(document.RootElement);
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
                WriteRedacted(document.RootElement, writer);

            var text = Encoding.UTF8.GetString(stream.ToArray());
            return new PayloadDetails(requestId, Truncate(text));
        }
        catch (JsonException)
        {
            // Invalid JSON still needs to be visible in the audit log so the request can be diagnosed.
            var text = Encoding.UTF8.GetString(payload)
                .Replace("\r", "\\r", StringComparison.Ordinal)
                .Replace("\n", "\\n", StringComparison.Ordinal);
            return new PayloadDetails(null, Truncate(text));
        }
    }

    private static Guid? FindRequestId(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
            return null;

        foreach (var property in root.EnumerateObject())
        {
            if (property.NameEquals("RequestId") ||
                property.Name.Equals("RequestId", StringComparison.OrdinalIgnoreCase))
            {
                return property.Value.ValueKind == JsonValueKind.String &&
                       property.Value.TryGetGuid(out var requestId)
                    ? requestId
                    : null;
            }
        }

        return null;
    }

    private static void WriteRedacted(JsonElement element, Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject())
                {
                    writer.WritePropertyName(property.Name);
                    if (IsSensitive(property.Name))
                        writer.WriteStringValue(RedactedValue);
                    else
                        WriteRedacted(property.Value, writer);
                }
                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                    WriteRedacted(item, writer);
                writer.WriteEndArray();
                break;

            default:
                element.WriteTo(writer);
                break;
        }
    }

    private static bool IsSensitive(string propertyName) =>
        propertyName.Equals("SessionId", StringComparison.OrdinalIgnoreCase) ||
        propertyName.Equals("Authorization", StringComparison.OrdinalIgnoreCase) ||
        propertyName.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
        propertyName.Contains("Secret", StringComparison.OrdinalIgnoreCase) ||
        propertyName.EndsWith("Token", StringComparison.OrdinalIgnoreCase);

    private static string Truncate(string value) =>
        value.Length <= MaxPayloadLogLength
            ? value
            : $"{value[..MaxPayloadLogLength]}...<truncated; totalChars={value.Length}>";

    private static object SafeRemoteEndPoint(ClientConnection connection)
    {
        try
        {
            return connection.RemoteEndPoint;
        }
        catch (ObjectDisposedException)
        {
            return "<disconnected>";
        }
        catch (SocketException)
        {
            return "<unavailable>";
        }
    }

    private sealed record PayloadDetails(Guid? RequestId, string Text);
}
