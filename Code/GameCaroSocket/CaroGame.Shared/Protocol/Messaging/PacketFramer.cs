using CaroGame.Shared.Networking.Messaging;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace CaroGame.Infrastructure.Networking.Messaging
{
    public class PacketFramer : IPacketFramer
    {
        private readonly int HeaderSize = sizeof(int); 
        private readonly int MessageTypeSize = sizeof(byte);
        private readonly int MaxPayloadSize = 8* 1024 * 1024; 
        public byte[] Frame(Packet packet)
        {
            int bodyLength = packet.Length + MessageTypeSize;
            byte[] buffer = new byte[HeaderSize + bodyLength];

            BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(0, HeaderSize), bodyLength);
            buffer[HeaderSize] = (byte)packet.MessageType;
            packet.Payload.CopyTo(buffer, HeaderSize + MessageTypeSize);
            return buffer;
        }
        public async Task<Packet?> ReadPacketAsync(Socket clientSocket, CancellationToken cancellationToken)
        {
            
            
            byte[]? headerBuffer = await ReadExactlyAsync(clientSocket, HeaderSize, cancellationToken);  

            if(headerBuffer is null)
            {
                return null; 
            }

            int bodyLength = BinaryPrimitives.ReadInt32BigEndian(headerBuffer);

            if (bodyLength < 0 || bodyLength > MaxPayloadSize)
            {
                throw new InvalidDataException("Invalid payload length");
            }


            byte[]? messageTypeBuffer = await ReadExactlyAsync(clientSocket, MessageTypeSize, cancellationToken);

            if (messageTypeBuffer is null)
            {
                throw new InvalidDataException("Connection closed before receiving payload");
            }

            byte messageTypeValue = messageTypeBuffer[0];
            if(!Enum.IsDefined(typeof(MessageTypes), messageTypeValue))
            {
                throw new InvalidDataException("Invalid message type");
            }
            var messageType = (MessageTypes)messageTypeValue;


            byte[]? payloadBuffer = await ReadExactlyAsync(clientSocket, bodyLength - MessageTypeSize, cancellationToken);
            
            //Buffer.BlockCopy(messageTypeBuffer, MessageTypeSize, payloadBuffer, 0, payloadBuffer.Length);

            return new Packet(payloadBuffer, messageType);

        }

        private async Task<byte[]?> ReadExactlyAsync(Socket socket, int size, CancellationToken cancellationToken)
        {
            if (size == 0) return Array.Empty<byte>();

            byte[] buffer = new byte[size];
            int totalBytesRead = 0;

            while(totalBytesRead < size)
            {
                int bytesRead = await socket.ReceiveAsync(buffer.AsMemory(totalBytesRead, size - totalBytesRead), SocketFlags.None, cancellationToken);
                if (bytesRead == 0)
                {
                    if(totalBytesRead == 0)
                    {
                        return null;
                    }
                    
                    throw new InvalidDataException("Connection closed before receiving all data");
                    
                }
                totalBytesRead += bytesRead;
            }
            return buffer;
        }

        public async Task WriteAsync(Socket socket, Packet packet, CancellationToken cancellationToken)
        {
            byte[] buffer = Frame(packet);

            await SendExactlyAsync(socket, buffer, cancellationToken);
        }

        private async Task SendExactlyAsync(Socket socket, byte[] buffer, CancellationToken cancellationToken)
        {
            int totalBytesSent = 0;

            while(totalBytesSent < buffer.Length)
            {
                int bytesSent = await socket.SendAsync(buffer.AsMemory(totalBytesSent), SocketFlags.None, cancellationToken);
                if (bytesSent == 0)
                {
                    throw new InvalidOperationException("Connection closed before sending all data");
                }
                totalBytesSent += bytesSent;
            }


        }

        
    }
}
