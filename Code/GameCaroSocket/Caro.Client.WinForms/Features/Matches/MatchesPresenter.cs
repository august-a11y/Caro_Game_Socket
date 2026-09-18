using Caro.Client.WinForms.Core;
using Caro.Client.WinForms.Services;
using CaroGame.Shared.Protocol.Contracts;

namespace Caro.Client.WinForms.Features.Matches;

internal sealed class MatchesPresenter : IDisposable
{
    private readonly FMain _view;
    private readonly LobbyClientService _lobby;
    private readonly RoomClientService? _rooms;
    private readonly ClientStateStore _state;
    private readonly CancellationTokenSource _stop = new();
    private Task _refresh = Task.CompletedTask;
    private int _watchInFlight;
    private bool _disposed;

    public MatchesPresenter(FMain view, LobbyClientService lobby, ClientStateStore state,
        RoomClientService? rooms = null)
    {
        _view = view ?? throw new ArgumentNullException(nameof(view));
        _lobby = lobby ?? throw new ArgumentNullException(nameof(lobby));
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _rooms = rooms;
        view.MatchesRequested += RefreshRequested;
        view.WatchMatchRequested += WatchMatchRequested;
    }

    public Task RefreshAsync()
    {
        if (!_refresh.IsCompleted) return _refresh;
        return _refresh = RefreshCoreAsync();
    }

    private async Task RefreshCoreAsync()
    {
        try
        {
            var matches = await _lobby.GetActiveMatchesAsync(_stop.Token);
            if (_stop.IsCancellationRequested || !_state.Current.IsJoined) return;
            _view.DisplayMatches(matches.Select(match => new MatchRow(
                match.RoomId,
                match.PlayerXName,
                match.PlayerOName,
                match.SpectatorCount,
                match.Status)));
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
        catch (Exception error) when (!_stop.IsCancellationRequested)
        {
            _view.DisplayMatchesError(error.Message);
        }
    }

    private async void RefreshRequested() => await RefreshAsync();

    private async void WatchMatchRequested(Guid roomId)
    {
        if (_rooms is null)
        {
            _view.DisplayStatus("Chức năng xem trận chưa được khởi tạo.");
            return;
        }

        if (Interlocked.Exchange(ref _watchInFlight, 1) != 0)
            return;

        try
        {
            _view.DisplayStatus("Đang tham gia với vai trò khán giả...");
            await _rooms.JoinAsSpectatorAsync(roomId, _stop.Token);
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
        catch (Exception error)
        {
            if (!_stop.IsCancellationRequested) _view.DisplayStatus(error.Message);
        }
        finally { Interlocked.Exchange(ref _watchInFlight, 0); }
    }

    private void JoinedAsSpectator(RoomSnapshot room)
    {
        _view.OpenRoom(room.PlayerXName ?? "Người chơi X",
            room.PlayerOName ?? "Người chơi O", room.Status);
        _view.DisplayGameHint("Đang xem trận đấu.");
        _view.DisplayGameBoard(ToBoard(room.Board), canMove: false);
        _view.DisplaySpectators(room.SpectatorCount);
    }

    private static string?[,] ToBoard(string?[][] board)
    {
        if (board.Length == 0)
            return new string?[15, 15];
        if (board.Length != 15 || board.Any(row => row.Length != 15))
            throw new InvalidDataException("Kích thước bàn cờ từ máy chủ không hợp lệ.");
        var result = new string?[15, 15];
        for (var row = 0; row < 15; row++)
            for (var column = 0; column < 15; column++)
                result[row, column] = board[row][column];
        return result;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _stop.Cancel();
        _view.MatchesRequested -= RefreshRequested;
        _view.WatchMatchRequested -= WatchMatchRequested;
    }
}
