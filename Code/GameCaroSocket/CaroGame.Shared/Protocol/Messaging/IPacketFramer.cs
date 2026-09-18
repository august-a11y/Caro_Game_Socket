using CaroGame.Shared.Networking.Messaging;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace CaroGame.Shared.Networking.Messaging
{
    public interface IPacketFramer
    {
        Task<Packet?> ReadPacketAsync(Socket socket, CancellationToken cancellationToken);
        Task WriteAsync(Socket socket, Packet packet, CancellationToken cancellation);
    }
}
