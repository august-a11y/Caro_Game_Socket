using System.Net.Sockets;

public class ConnectionContext
{
    public Guid ConnectionId { get; init; } = Guid.NewGuid();
    public Socket Socket { get; }
    public Guid SessionId { get; init; }
    public ConnectionContext(Socket socket)
    {
        Socket = socket;
    }

}