namespace CaroGame.Shared.Networking;

public sealed record ServerInfo
{
    public const string ServiceName = "CaroGame.Server:ServerUTH";
    public const int DiscoveryPort = 5001;
    public const int TcpServerPort = 5000;

    public string Service { get; init; } = ServiceName;

    public string ServerName { get; init; } = string.Empty;

    public string Host { get; init; } = string.Empty;

    public int TcpPort { get; init; } = TcpServerPort;

    public bool IsValid()
    {
        return string.Equals(
                   Service,
                   ServiceName,
                   StringComparison.Ordinal) &&
               !string.IsNullOrWhiteSpace(Host) &&
               TcpPort is >= 1 and <= 65535;
    }
}
