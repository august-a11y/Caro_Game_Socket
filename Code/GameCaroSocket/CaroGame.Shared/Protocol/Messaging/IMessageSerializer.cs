using System;
using System.Collections.Generic;
using System.Text;

namespace CaroGame.Shared.Networking.Messaging
{
    public interface IMessageSerializer
    {
        byte[] Serialize<T>(T message) ;
        T? Deserialize<T>(byte[] data) ;
    }
}
