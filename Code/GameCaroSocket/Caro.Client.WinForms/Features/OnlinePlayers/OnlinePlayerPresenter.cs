using Caro.Client.WinForms.Core;
using Caro.Client.WinForms.Networking;
using Caro.Client.WinForms.Services;

namespace Caro.Client.WinForms.Features.OnlinePlayers;

internal sealed class OnlinePlayerPresenter : IDisposable
{
    private readonly FMain _view;
    private readonly LobbyClientService _service;
    private readonly ClientStateStore _state;
    private readonly TcpGameClient _connection;
    private readonly CancellationTokenSource _stop = new();
    private readonly object _sync = new();
    private readonly Dictionary<Guid, CaroGame.Shared.Protocol.Contracts.PlayerDto> _players = new();
    private Task _refresh = Task.CompletedTask;
    private int _connectionVersion;

    public OnlinePlayerPresenter(FMain view, LobbyClientService service, ClientStateStore state, TcpGameClient connection)
    {
        _view = view;
        _service = service;
        _state = state;
        _connection = connection;
        view.OnlinePlayersRequested += RefreshRequested;
        connection.Disconnected += Disconnected;
        service.PlayerOnline += PlayerUpserted;
        service.PlayerStatusChanged += PlayerUpserted;
        service.PlayerOffline += PlayerRemoved;
    }

    public Task RefreshAsync()
    {
        if (!_refresh.IsCompleted) return _refresh;
        return _refresh = RefreshCoreAsync();
    }

    private async Task RefreshCoreAsync()
    {
        int version = Volatile.Read(ref _connectionVersion);
        try
        {
            _view.DisplayOnlinePlayersLoading();
            var players = await _service.GetOnlinePlayersAsync(_stop.Token);
            if (_stop.IsCancellationRequested || version != Volatile.Read(ref _connectionVersion) || !_state.Current.IsJoined) return;
            lock (_sync)
            {
                _players.Clear();
                foreach (var player in players) _players[player.PlayerId] = player;
            }
            Render();
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
        catch (Exception error)
        {
            if (!_stop.IsCancellationRequested && version == Volatile.Read(ref _connectionVersion) && _state.Current.IsJoined)
                _view.DisplayOnlinePlayersError(error.Message);
        }
    }

    private async void RefreshRequested() => await RefreshAsync();

    private void PlayerUpserted(CaroGame.Shared.Protocol.Contracts.PlayerDto player)
    {
        lock (_sync) _players[player.PlayerId] = player;
        Render();
    }

    private void PlayerRemoved(Guid playerId)
    {
        lock (_sync) _players.Remove(playerId);
        Render();
    }

    private void Render()
    {
        if (!_state.Current.IsJoined || _stop.IsCancellationRequested) return;
        var self = _state.Current.PlayerId;
        CaroGame.Shared.Protocol.Contracts.PlayerDto[] snapshot;
        lock (_sync) snapshot = _players.Values
            .Where(player => player.PlayerId != self && player.Status != "Offline")
            .OrderBy(player => player.Nickname, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
        _view.DisplayOnlinePlayers(snapshot.Select(player => new OnlinePlayerRow(
            player.PlayerId, player.Nickname,
            player.Status switch { "Free" => "Rảnh", "InMatch" => "Đang chơi", _ => "Không khả dụng" },
            0, 0)));
    }

    private void Disconnected()
    {
        Interlocked.Increment(ref _connectionVersion);
        lock (_sync) _players.Clear();
    }

    public void Dispose()
    {
        _stop.Cancel();
        _view.OnlinePlayersRequested -= RefreshRequested;
        _connection.Disconnected -= Disconnected;
        _service.PlayerOnline -= PlayerUpserted;
        _service.PlayerStatusChanged -= PlayerUpserted;
        _service.PlayerOffline -= PlayerRemoved;
    }
}
