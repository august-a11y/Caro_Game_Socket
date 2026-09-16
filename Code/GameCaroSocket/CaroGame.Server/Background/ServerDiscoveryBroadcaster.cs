using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using CaroGame.Server.Background;

public sealed class ServerDiscoveryBroadcaster(ServerDiscoveryBroadcastOptions options, TimeProvider time)
{
    private readonly int _port = 5001;

    
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        options.Validate();
        using var timer = new PeriodicTimer(options.CheckInterval, time);
        try
        {
            // Await each scan so ticks never overlap within this worker.
            while (await timer.WaitForNextTickAsync(cancellationToken))
                await StartAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
    }
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var udp = new UdpClient();

        udp.EnableBroadcast = true;

        var message = new
        {
            Type = "CaroServer",
            ServerName = "Caro Server",
            IpAddress = GetLocalIpAddress(),
            TcpPort = 5000
        };

        byte[] data = Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(message));

        var endpoint = new IPEndPoint(
            IPAddress.Broadcast,
            _port);

        while (!cancellationToken.IsCancellationRequested)
        {
            await udp.SendAsync(data, endpoint);

            await Task.Delay(
                TimeSpan.FromSeconds(3),
                cancellationToken);
        }
    }

    private static string GetLocalIpAddress()
    {
        return Dns.GetHostEntry(Dns.GetHostName())
            .AddressList
            .First(ip => ip.AddressFamily == AddressFamily.InterNetwork)
            .ToString();
    }
}