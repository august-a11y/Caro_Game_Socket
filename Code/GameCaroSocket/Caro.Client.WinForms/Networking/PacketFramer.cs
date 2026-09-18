using System.Buffers.Binary;
using CaroGame.Shared.Networking.Messaging;

namespace Caro.Client.WinForms.Networking;

internal sealed class PacketFramer
{
    private const int HeaderSize = sizeof(int);
    private const int MessageTypeSize = sizeof(byte);
    private const int MaxBodyLength = 8 * 1024 * 1024;

    public async Task<Packet?> ReadAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var header = await ReadExactlyAsync(stream, HeaderSize, cancellationToken);
        if (header is null)
            return null;

        var bodyLength = BinaryPrimitives.ReadInt32BigEndian(header);
        if (bodyLength < MessageTypeSize || bodyLength > MaxBodyLength)
            throw new InvalidDataException("Invalid packet body length.");

        var typeBuffer = await ReadExactlyAsync(stream, MessageTypeSize, cancellationToken)
            ?? throw new InvalidDataException("Connection closed before receiving message type.");
        if (!Enum.IsDefined(typeof(MessageTypes), typeBuffer[0]))
            throw new InvalidDataException($"Invalid message type '{typeBuffer[0]}'.");

        var payload = await ReadExactlyAsync(stream, bodyLength - MessageTypeSize, cancellationToken)
            ?? throw new InvalidDataException("Connection closed before receiving payload.");
        return new Packet(payload, (MessageTypes)typeBuffer[0]);
    }

    public async Task WriteAsync(Stream stream, Packet packet, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(packet);
        var bodyLength = checked(MessageTypeSize + packet.Payload.Length);
        if (bodyLength > MaxBodyLength)
            throw new InvalidDataException("Packet payload exceeds the 8 MB limit.");

        var frame = new byte[HeaderSize + bodyLength];
        BinaryPrimitives.WriteInt32BigEndian(frame.AsSpan(0, HeaderSize), bodyLength);
        frame[HeaderSize] = (byte)packet.MessageType;
        packet.Payload.CopyTo(frame, HeaderSize + MessageTypeSize);
        await stream.WriteAsync(frame, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    private static async Task<byte[]?> ReadExactlyAsync(Stream stream, int length, CancellationToken cancellationToken)
    {
        var buffer = new byte[length];
        var offset = 0;
        while (offset < length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset, length - offset), cancellationToken);
            if (read == 0)
            {
                if (offset == 0)
                    return null;
                throw new InvalidDataException("Connection closed before receiving a complete packet.");
            }
            offset += read;
        }
        return buffer;
    }
}
