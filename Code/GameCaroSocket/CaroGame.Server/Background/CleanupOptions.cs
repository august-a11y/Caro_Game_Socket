namespace CaroGame.Server.Background;

public sealed class CleanupOptions
{
    public TimeSpan CheckInterval { get; init; } = TimeSpan.FromSeconds(10);
    public TimeSpan RoomRetention { get; init; } = TimeSpan.FromMinutes(15);
    public TimeSpan ChallengeRetention { get; init; } = TimeSpan.FromMinutes(15);
    public TimeSpan OfflinePlayerRetention { get; init; } = TimeSpan.FromMinutes(1);

    public bool RetainsRoom(DateTime closedAt, DateTime now) => now - closedAt < RoomRetention;

    internal void Validate()
    {
        if (CheckInterval.TotalMilliseconds < 1 || CheckInterval.TotalMilliseconds > uint.MaxValue - 1)
            throw new ArgumentOutOfRangeException(nameof(CheckInterval));
        if (RoomRetention <= TimeSpan.Zero || ChallengeRetention <= TimeSpan.Zero || OfflinePlayerRetention <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(RoomRetention), "Retention durations must be positive.");
    }
}
