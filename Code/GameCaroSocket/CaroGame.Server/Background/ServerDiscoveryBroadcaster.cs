using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;
using CaroGame.Server.Background;
using CaroGame.Shared.Networking;

public sealed class ServerDiscoveryBroadcaster(
    ServerDiscoveryBroadcastOptions options,
    TimeProvider time)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        options.Validate();

        using var udp = new UdpClient
        {
            EnableBroadcast = true
        };

        using var timer = new PeriodicTimer(
            options.CheckInterval,
            time);

        var endpoint = new IPEndPoint(
            IPAddress.Broadcast,
            ServerInfo.DiscoveryPort);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var discoveryInfo = new ServerInfo
                {
                    Service = ServerInfo.ServiceName,
                    ServerName = ServerInfo.ServiceName,
                    Host = GetLocalIpAddress(),
                    TcpPort = ServerInfo.TcpServerPort
                };

                byte[] data = JsonSerializer.SerializeToUtf8Bytes(
                    discoveryInfo);

                await udp.SendAsync(
                    data,
                    data.Length,
                    endpoint);

                // Chờ đến chu kỳ broadcast tiếp theo.
                await timer.WaitForNextTickAsync(
                    cancellationToken);
            }
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            // Server đang dừng bình thường.

        }
    }

    private static string GetLocalIpAddress()
    {

        var networkInterfaces = NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(networkInterface =>
                networkInterface.OperationalStatus ==
                OperationalStatus.Up &&
                networkInterface.NetworkInterfaceType !=
                NetworkInterfaceType.Loopback);

        foreach (var networkInterface in networkInterfaces)
        {
            var address = networkInterface
                .GetIPProperties()
                .UnicastAddresses
                .Select(unicast => unicast.Address)
                .FirstOrDefault(ip =>
                    ip.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(ip));

            if (address is not null)
                return address.ToString();
        }

        throw new InvalidOperationException(
            "Không tìm thấy địa chỉ IPv4 LAN của server.");
    }
}

