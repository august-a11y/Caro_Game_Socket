namespace CaroGame.Infrastructure.Networking.Messaging
{
    public class MessageSerializer : IMessageSerializer
    {
        public T Deserialize<T>(byte[] data)
        {
            var dataJson = System.Text.Encoding.UTF8.GetString(data);
            return System.Text.Json.JsonSerializer.Deserialize<T>(dataJson);
        }

        public byte[] Serialize<T>(T message)
        {
            var dataJson = System.Text.Json.JsonSerializer.Serialize(message);
            return System.Text.Encoding.UTF8.GetBytes(dataJson);
        }
    }
}
