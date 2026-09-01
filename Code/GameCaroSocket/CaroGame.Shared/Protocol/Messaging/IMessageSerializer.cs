using System;
using System.Collections.Generic;
using System.Text;

namespace CaroGame.Infrastructure.Networking.Messaging
{
    public interface IMessageSerializer
    {
        byte[] Serialize<T>(T message) ;
        T Deserialize<T>(byte[] data) ;
    }
}
