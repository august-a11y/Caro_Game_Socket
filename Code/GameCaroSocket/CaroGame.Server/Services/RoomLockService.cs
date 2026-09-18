

public class RoomLockService
{
    private readonly Dictionary<Guid, Entry> _lockedRooms = new();
    private readonly object _sync = new();

    private sealed class Entry
    {
        public readonly SemaphoreSlim Gate = new(1, 1);
        public int Users; 
    }

    public async Task LockRoomAsync(Guid roomId, CancellationToken cancellationToken = default)
    {
        Entry entry;
     
        lock (_sync)
        {
            if (!_lockedRooms.TryGetValue(roomId, out entry!))
                _lockedRooms.Add(roomId, entry = new Entry());
            entry.Users++;
        }
        try { await entry.Gate.WaitAsync(cancellationToken); }
        catch
        {
            lock (_sync) ReleaseReference(roomId, entry);
            throw;
        }
    }

    public void UnlockRoom(Guid roomId)
    {
        lock (_sync)
        {
            if (!_lockedRooms.TryGetValue(roomId, out var entry))
                throw new InvalidOperationException("The room lock was not acquired.");
            entry.Gate.Release();
            ReleaseReference(roomId, entry);
        }
    }

    private void ReleaseReference(Guid roomId, Entry entry)
    {
        if (--entry.Users != 0) return;
        _lockedRooms.Remove(roomId);
        entry.Gate.Dispose();
    }
}
