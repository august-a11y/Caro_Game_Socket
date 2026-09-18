namespace CaroGame.Server.Services;

// Serializes session transfers and challenge creation/acceptance across connections.
// Gameplay continues to use the separate per-room locks.
public sealed class LobbyLockService : IDisposable
{
    public SemaphoreSlim Gate { get; } = new(1, 1);
    public void Dispose() => Gate.Dispose();
}
