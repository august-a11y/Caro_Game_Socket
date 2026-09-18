namespace CaroGame.Shared.Networking.Messaging
{
    public class Packet
    {
        public int Length => Payload.Length;
        public MessageTypes MessageType { get; set; }
        public byte[] Payload { get; set; }
        public Packet( byte[] payload, MessageTypes messageType)
        {
            Payload = payload;
            MessageType = messageType;
        }
    }

    
}