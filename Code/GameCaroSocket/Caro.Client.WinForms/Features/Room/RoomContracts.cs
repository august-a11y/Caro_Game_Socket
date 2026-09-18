using CaroGame.Shared.Protocol.Contracts;

namespace Caro.Client.WinForms.Features.Room;

public interface IRoomClientService
{
    Task ReadyAsync(Guid roomId, CancellationToken cancellationToken = default);
    Task LeaveWaitingRoomAsync(Guid roomId, CancellationToken cancellationToken = default);
    Task JoinAsSpectatorAsync(Guid roomId, CancellationToken cancellationToken = default);
    Task LeaveAsSpectatorAsync(Guid roomId, CancellationToken cancellationToken = default);
}

public interface IRoomView
{
    event Action? ReadyRequested;
    event Action? LeaveWaitingRoomRequested;
    event Action? JoinAsSpectatorRequested;
    event Action? LeaveAsSpectatorRequested;
    void DisplayRoom(RoomSnapshot room, Guid? currentPlayerId);
    void DisplayError(string message);
}
