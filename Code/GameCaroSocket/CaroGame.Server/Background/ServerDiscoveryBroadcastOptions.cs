namespace CaroGame.Server.Background;

public sealed class ServerDiscoveryBroadcastOptions
{
    public TimeSpan CheckInterval { get; init; } = TimeSpan.FromSeconds(1);


    internal void Validate()
    {
        if (CheckInterval <= TimeSpan.Zero || CheckInterval.TotalMilliseconds > uint.MaxValue - 1)
            throw new ArgumentOutOfRangeException(nameof(CheckInterval));
    
    }
}