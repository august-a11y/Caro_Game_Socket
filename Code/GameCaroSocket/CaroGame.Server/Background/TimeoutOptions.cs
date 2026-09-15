namespace CaroGame.Server.Background;

public sealed class TimeoutOptions
{
    public TimeSpan CheckInterval { get; init; } = TimeSpan.FromSeconds(1);
    public TimeSpan HeartbeatTimeout { get; init; } = TimeSpan.FromSeconds(30);

    internal void Validate()
    {
        if (CheckInterval <= TimeSpan.Zero || CheckInterval.TotalMilliseconds > uint.MaxValue - 1)
            throw new ArgumentOutOfRangeException(nameof(CheckInterval));
        if (HeartbeatTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(HeartbeatTimeout));
    }
}
