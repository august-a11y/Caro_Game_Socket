using CaroGame.Shared.Protocol.Contracts;

namespace Caro.Client.WinForms.Features.Room;

internal sealed class RoomPresenter : IDisposable
{
    private readonly IRoomView _view;
    private readonly IRoomClientService _service;
    private readonly Action<RoomSnapshot> _openGame;
    private Guid? _roomId;
    private Guid? _currentPlayerId;
    private bool _disposed;

    public RoomPresenter(IRoomView view, IRoomClientService service, Action<RoomSnapshot> openGame)
    {
        _view = view ?? throw new ArgumentNullException(nameof(view));
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _openGame = openGame ?? throw new ArgumentNullException(nameof(openGame));
        _view.ReadyRequested += ReadyRequested;
        _view.LeaveWaitingRoomRequested += LeaveWaitingRoomRequested;
        _view.JoinAsSpectatorRequested += JoinAsSpectatorRequested;
        _view.LeaveAsSpectatorRequested += LeaveAsSpectatorRequested;
    }

    public void Open(RoomSnapshot room, Guid? currentPlayerId)
    {
        ArgumentNullException.ThrowIfNull(room);
        _roomId = room.RoomId;
        _currentPlayerId = currentPlayerId;
        _view.DisplayRoom(room, currentPlayerId);
    }

    public void HandleRoomSnapshot(RoomSnapshot room)
    {
        if (_roomId == room.RoomId)
            _view.DisplayRoom(room, _currentPlayerId);
    }

    public void HandleMatchStarted(RoomSnapshot room)
    {
        if (_roomId != room.RoomId)
            return;
        _view.DisplayRoom(room, _currentPlayerId);
        _openGame(room);
    }

    private async void ReadyRequested() => await RunCommandAsync(id => _service.ReadyAsync(id));
    private async void LeaveWaitingRoomRequested() => await RunCommandAsync(id => _service.LeaveWaitingRoomAsync(id));
    private async void JoinAsSpectatorRequested() => await RunCommandAsync(id => _service.JoinAsSpectatorAsync(id));
    private async void LeaveAsSpectatorRequested() => await RunCommandAsync(id => _service.LeaveAsSpectatorAsync(id));

    private async Task RunCommandAsync(Func<Guid, Task> command)
    {
        if (_roomId is not Guid roomId)
        {
            _view.DisplayError("Chưa có phòng hiện tại.");
            return;
        }
        try { await command(roomId); }
        catch (Exception exception) { _view.DisplayError(exception.Message); }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _view.ReadyRequested -= ReadyRequested;
        _view.LeaveWaitingRoomRequested -= LeaveWaitingRoomRequested;
        _view.JoinAsSpectatorRequested -= JoinAsSpectatorRequested;
        _view.LeaveAsSpectatorRequested -= LeaveAsSpectatorRequested;
    }
}
