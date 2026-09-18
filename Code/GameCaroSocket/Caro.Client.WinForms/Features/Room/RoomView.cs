using CaroGame.Shared.Protocol.Contracts;
using Caro.Client.WinForms.Core;

namespace Caro.Client.WinForms;

public sealed class RoomView : UserControl, Features.Room.IRoomView
{
    private readonly Label _room = new() { AutoSize = true };
    private readonly Label _players = new() { AutoSize = true };
    private readonly Label _status = new() { AutoSize = true };
    private readonly Label _spectators = new() { AutoSize = true };
    private readonly Button _ready = new() { Text = "Sẵn sàng", AutoSize = true };
    private readonly Button _leave = new() { Text = "Rời phòng", AutoSize = true };
    private readonly Button _joinSpectator = new() { Text = "Tham gia xem", AutoSize = true };
    private readonly Button _leaveSpectator = new() { Text = "Rời chế độ xem", AutoSize = true };

    public event Action? ReadyRequested;
    public event Action? LeaveWaitingRoomRequested;
    public event Action? JoinAsSpectatorRequested;
    public event Action? LeaveAsSpectatorRequested;

    public RoomView()
    {
        Dock = DockStyle.Fill;
        var layout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(24)
        };
        layout.Controls.AddRange([_room, _players, _status, _spectators,
            _ready, _leave, _joinSpectator, _leaveSpectator]);
        Controls.Add(layout);
        _ready.Click += (_, _) => ReadyRequested?.Invoke();
        _leave.Click += (_, _) => LeaveWaitingRoomRequested?.Invoke();
        _joinSpectator.Click += (_, _) => JoinAsSpectatorRequested?.Invoke();
        _leaveSpectator.Click += (_, _) => LeaveAsSpectatorRequested?.Invoke();
    }

    public void DisplayRoom(RoomSnapshot room, Guid? currentPlayerId)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => DisplayRoom(room, currentPlayerId));
            return;
        }

        var isPlayer = currentPlayerId == room.PlayerXId || currentPlayerId == room.PlayerOId;
        _room.Text = "Phòng chơi";
        _players.Text = $"X: {room.PlayerXName ?? "Người chơi X"} | O: {room.PlayerOName ?? "Người chơi O"}";
        _status.Text =
            $"Trạng thái: {UiText.RoomStatus(room.Status)} | Sẵn sàng: {room.ReadyPlayers.Count}/2";
        _spectators.Text = $"Khán giả: {room.SpectatorCount}";
        _ready.Enabled = room.Status == "Waiting" && isPlayer;
        _leave.Enabled = room.Status == "Waiting" && isPlayer;
        _joinSpectator.Enabled = room.Status == "Playing" && !isPlayer && currentPlayerId is not null;
        _leaveSpectator.Enabled = room.Status == "Playing" && !isPlayer && currentPlayerId is not null;
    }

    public void DisplayError(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => DisplayError(message));
            return;
        }
        MessageBox.Show(this, message, "Phòng chơi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
