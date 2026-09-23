using Caro.Client.WinForms.Core;
using Caro.Client.WinForms.Services;
using CaroGame.Shared.Protocol.Contracts;

namespace Caro.Client.WinForms.Features.Game;

internal sealed class GamePresenter : IDisposable
{
    private readonly FMain _view;
    private readonly RoomClientService _rooms;
    private readonly GameplayClientService _gameplay;
    private readonly ClientStateStore _state;
    private Guid? _roomId;
    private string _status = "Waiting";
    private Guid? _turnPlayerId;
    private Guid _playerXId;
    private Guid _playerOId;
    private string _playerXName = "Người chơi X";
    private string _playerOName = "Người chơi O";
    private string?[,] _board = new string?[15, 15];
    private readonly HashSet<Guid> _readyPlayers = new();
    private bool _spectator;
    private bool _readyInFlight;
    private bool _moveInFlight;
    private bool _leaveInFlight;
    private bool _surrenderInFlight;
    private bool _rematchInFlight;
    private bool _resyncInFlight;
    private int _lastMoveNumber;
    private bool _disposed;

    public GamePresenter(FMain view, RoomClientService rooms,
        GameplayClientService gameplay, ClientStateStore state)
    {
        _view = view ?? throw new ArgumentNullException(nameof(view));
        _rooms = rooms ?? throw new ArgumentNullException(nameof(rooms));
        _gameplay = gameplay ?? throw new ArgumentNullException(nameof(gameplay));
        _state = state ?? throw new ArgumentNullException(nameof(state));

        view.ReadyRequested += ReadyRequested;
        view.RematchRequested += RematchRequested;
        view.SurrenderRequested += SurrenderRequested;
        view.MoveRequested += MoveRequested;
        view.LeaveRoomRequested += LeaveRequested;
        rooms.PlayerReady += PlayerReady;
        rooms.MatchStarted += MatchStarted;
        rooms.JoinedAsSpectator += JoinedAsSpectator;
        rooms.SpectatorLeft += SpectatorLeft;
        rooms.RoomCancelled += RoomCancelled;
        rooms.WaitingRoomUpdated += WaitingRoomUpdated;
        rooms.RematchOffered += RematchOffered;
        rooms.RematchResponded += RematchResponded;
        gameplay.MoveReceived += MoveReceived;
        gameplay.GameOverReceived += GameOverReceived;
        gameplay.BoardStateReceived += BoardStateReceived;
        gameplay.MatchPaused += MatchPaused;
        gameplay.MatchResumed += MatchResumed;
        gameplay.TurnTimedOut += TurnTimedOut;
    }

    public void Open(RoomSnapshot room, bool spectator = false)
    {
        _roomId = room.RoomId;
        _status = room.Status;
        var currentPlayerId = _state.Current.PlayerId;
        var isPlayer = currentPlayerId is Guid playerId &&
            (playerId == room.PlayerXId || playerId == room.PlayerOId);
        _spectator = spectator || !isPlayer;
        _readyInFlight = false;
        _moveInFlight = false;
        _leaveInFlight = false;
        _surrenderInFlight = false;
        _rematchInFlight = false;
        _resyncInFlight = false;
        _board = ToBoard(room.Board);
        UpdatePlayerNames(room);
        _readyPlayers.Clear();
        foreach (var readyId in room.ReadyPlayers) _readyPlayers.Add(readyId);
        _view.OpenRoom(_playerXName, _playerOName, room.Status);
        ApplySnapshot(room);
    }

    private async void ReadyRequested()
    {
        if (_roomId is not Guid roomId || _spectator || _status != "Waiting" ||
            _readyInFlight)
            return;
        _readyInFlight = true;
        _view.DisplayReadyState(isReady: false, canReady: false);
        _view.DisplayStatus("Đang gửi trạng thái sẵn sàng...");
        try { await _rooms.ReadyAsync(roomId); }
        catch (Exception error)
        {
            _readyInFlight = false;
            var playerId = _state.Current.PlayerId;
            var isReady = playerId is Guid current && _readyPlayers.Contains(current);
            _view.DisplayReadyState(isReady,
                canReady: _status == "Waiting" && !_spectator && !isReady);
            _view.DisplayStatus(error.Message);
        }
    }

    private async void MoveRequested(int row, int column)
    {
        if (_roomId is not Guid roomId || _spectator || _status != "Playing" ||
            !CanMove(_turnPlayerId) || _moveInFlight)
            return;
        _moveInFlight = true;
        try { await _gameplay.SubmitMoveAsync(roomId, row, column); }
        catch (Exception error)
        {
            _moveInFlight = false;
            _view.DisplayMoveError(error.Message, CanMove(_turnPlayerId));
        }
    }

    private async void LeaveRequested()
    {
        if (_roomId is not Guid roomId || _leaveInFlight) return;
        _leaveInFlight = true;
        _view.DisplayStatus("Đang rời phòng...");
        try
        {
            if (_spectator)
                await _rooms.LeaveAsSpectatorAsync(roomId);
            else if (CurrentStatus() == "Finished")
            {
                _view.CloseRoom();
                _roomId = null;
                _leaveInFlight = false;
            }
            else if (CurrentStatus() == "Waiting")
                await _rooms.LeaveWaitingRoomAsync(roomId);
            else
            {
                _leaveInFlight = false;
                _view.DisplayStatus("Không thể rời phòng khi trận đang diễn ra. Hãy dùng nút Đầu hàng.");
            }
        }
        catch (Exception error)
        {
            _leaveInFlight = false;
            _view.DisplayStatus(error.Message);
        }
    }

    private async void SurrenderRequested()
    {
        if (_roomId is not Guid roomId || _spectator || _status != "Playing" ||
            _surrenderInFlight)
            return;

        _surrenderInFlight = true;
        _view.DisplaySurrenderState(canSurrender: false);
        _view.DisplayStatus("Đang gửi yêu cầu đầu hàng...");
        try
        {
            await _gameplay.SurrenderAsync(roomId);
        }
        catch (Exception error)
        {
            _surrenderInFlight = false;
            _view.DisplaySurrenderState(canSurrender: _status == "Playing" && !_spectator);
            _view.DisplayStatus(error.Message);
        }
    }

    private async void RematchRequested()
    {
        if (_roomId is not Guid roomId || _spectator || _status != "Finished" || _rematchInFlight)
            return;
        _rematchInFlight = true;
        _view.DisplayRematchState(canRequest: false, accepted: true);
        try
        {
            await _rooms.RespondToRematchAsync(roomId, accept: true);
        }
        catch (Exception error)
        {
            _rematchInFlight = false;
            _view.DisplayRematchState(canRequest: true, accepted: false);
            _view.DisplayStatus(error.Message);
        }
    }

    private void PlayerReady(RoomPlayerNotification notification)
    {
        if (_roomId != notification.RoomId) return;
        if (_state.Current.PlayerId == notification.PlayerId)
            _readyInFlight = false;
        _readyPlayers.Add(notification.PlayerId);
        var currentPlayerId = _state.Current.PlayerId;
        var isReady = currentPlayerId is Guid current && _readyPlayers.Contains(current);
        _view.DisplayReadyState(
            isReady,
            canReady: _status == "Waiting" && !_spectator && !isReady && !_readyInFlight);
        _view.DisplaySpectators(notification.SpectatorCount);
    }

    private void MatchStarted(RoomSnapshot room)
    {
        if (_roomId is null || _roomId == room.RoomId)
            Open(room, _spectator);
    }

    private void JoinedAsSpectator(RoomSnapshot room)
    {
        Open(room, spectator: true);
        _view.DisplayGameHint("Đang xem trận đấu.");
    }

    private void WaitingRoomUpdated(RoomSnapshot room)
    {
        if (_roomId == room.RoomId) ApplySnapshot(room);
    }

    private void RematchOffered(RematchOfferNotification notification)
    {
        if (_roomId == notification.RoomId && !_spectator && _status == "Finished")
            _view.DisplayRematchState(canRequest: true, accepted: false);
    }

    private void RematchResponded(RematchResponseNotification notification)
    {
        if (_roomId != notification.RoomId) return;
        if (_state.Current.PlayerId == notification.PlayerId)
        {
            _rematchInFlight = false;
            _view.DisplayRematchState(canRequest: false, accepted: notification.Accepted);
        }
        _view.DisplayGameHint(notification.Accepted
            ? $"Đã có {notification.AcceptedCount}/2 người đồng ý chơi lại."
            : "Đối thủ chưa đồng ý chơi lại.");
    }

    private void SpectatorLeft(RoomPlayerNotification notification)
    {
        if (_spectator && _roomId == notification.RoomId &&
            _state.Current.PlayerId == notification.PlayerId)
        {
            _view.CloseRoom();
            _roomId = null;
            _leaveInFlight = false;
        }
    }

    private void RoomCancelled(RoomCancelledNotification notification)
    {
        if (_roomId == notification.Room.RoomId)
        {
            _view.DisplayGameHint($"Phòng đã hủy: {UiText.ClosingReason(notification.Reason)}.");
            _view.CloseRoom();
            _roomId = null;
            _leaveInFlight = false;
        }
    }

    private void MoveReceived(MoveNotification notification)
    {
        if (_roomId != notification.RoomId) return;
        _moveInFlight = false;
        var move = notification.Move;
        if (move.MoveNumber <= _lastMoveNumber) return;
        if (move.MoveNumber != _lastMoveNumber + 1)
        {
            _ = ResyncBoardAsync(notification.RoomId);
            return;
        }
        if (move.Row is < 0 or >= 15 || move.Column is < 0 or >= 15) return;
        _board[move.Row, move.Column] = move.Symbol;
        _lastMoveNumber = move.MoveNumber;
        _turnPlayerId = notification.CurrentTurnPlayerId;
        _view.DisplayMove(move.Row, move.Column, move.Symbol,
            CanMove(notification.CurrentTurnPlayerId));
        string activeSymbol = notification.CurrentTurnPlayerId == _playerXId ? "X" : (notification.CurrentTurnPlayerId == _playerOId ? "O" : "");
        _view.DisplayGameTurn(
            CanMove(notification.CurrentTurnPlayerId) ? "Đến lượt bạn" : "Đang chờ đối thủ",
            Math.Max(0, (int)Math.Ceiling((notification.TurnDeadline - DateTime.UtcNow).TotalSeconds)),
            activeSymbol);
    }

    private void GameOverReceived(GameOverNotification notification)
    {
        if (_roomId != notification.Room.RoomId) return;
        _moveInFlight = false;
        _surrenderInFlight = false;
        _status = notification.Room.Status;
        _board = ToBoard(notification.Room.Board);
        _view.DisplayGameBoard(_board, canMove: false);
        if (!_spectator) _view.DisplayRematchState(canRequest: false, accepted: false);
        _view.DisplayGameActionMode(_spectator
            ? GameActionMode.Spectator
            : GameActionMode.FinishedPlayer);
        _view.DisplayGameTurn("Trận đấu đã kết thúc", 0);

        // Hiển thị người chiến thắng rõ ràng thay vì chỉ lý do
        string resultText = notification.Room.Result switch
        {
            "PlayerXWin" => $"Người chơi X ({notification.Room.PlayerXName}) thắng!",
            "PlayerOWin" => $"Người chơi O ({notification.Room.PlayerOName}) thắng!",
            "Draw" => "Ván đấu Hòa!",
            _ => UiText.MatchResult(notification.Room.Result)
        };
        string reasonText = UiText.ClosingReason(notification.Reason);
        _view.DisplayGameHint($"{resultText} ({reasonText})");

        // Xác định Thắng / Thua / Hòa
        bool isMyWin = false;
        bool isMyLoss = false;
        bool isDraw = notification.Room.Result == "Draw";
        
        if (!_spectator)
        {
            if (notification.Room.Result == "PlayerXWin")
            {
                isMyWin = _state.Current.PlayerId == _playerXId;
                isMyLoss = _state.Current.PlayerId == _playerOId;
            }
            else if (notification.Room.Result == "PlayerOWin")
            {
                isMyWin = _state.Current.PlayerId == _playerOId;
                isMyLoss = _state.Current.PlayerId == _playerXId;
            }
        }

        string popupTitle = "KẾT THÚC TRẬN ĐẤU";
        if (isMyWin) popupTitle = "BẠN ĐÃ THẮNG!";
        else if (isMyLoss) popupTitle = "BẠN ĐÃ THUA!";
        else if (isDraw) popupTitle = "VÁN ĐẤU HÒA!";

        // Bật Custom Popup
        _view.ShowGameOver(popupTitle, reasonText, isMyWin);
    }

    private void BoardStateReceived(RoomSnapshot room)
    {
        if (_roomId == room.RoomId) ApplySnapshot(room);
    }

    private async Task ResyncBoardAsync(Guid roomId)
    {
        if (_resyncInFlight) return;
        _resyncInFlight = true;
        try
        {
            var room = await _gameplay.GetBoardStateAsync(roomId);
            if (_roomId == room.RoomId) ApplySnapshot(room);
        }
        catch (Exception error)
        {
            _view.DisplayStatus(error.Message);
        }
        finally { _resyncInFlight = false; }
    }

    private void MatchPaused(MatchPausedNotification notification)
    {
        if (_roomId != notification.RoomId) return;
        _moveInFlight = false;
        ApplySnapshot(notification.Room);
        _view.DisplayGameHint($"Trận tạm dừng. Hạn kết nối lại: {notification.GracePeriodEndsAt.ToLocalTime():T}");
    }

    private void MatchResumed(RoomSnapshot room)
    {
        if (_roomId != room.RoomId) return;
        ApplySnapshot(room);
        _view.DisplayGameHint("Trận đấu đã tiếp tục.");
    }

    private void TurnTimedOut(TurnTimeoutNotification notification)
    {
        if (_roomId == notification.RoomId)
            _view.DisplayGameHint($"{PlayerName(notification.PlayerId)} đã hết giờ.");
    }

    private void ApplySnapshot(RoomSnapshot room)
    {
        _status = room.Status;
        _turnPlayerId = room.CurrentTurnPlayerId;
        _board = ToBoard(room.Board);
        UpdatePlayerNames(room);
        _lastMoveNumber = room.MoveHistory.Count == 0 ? 0 : room.MoveHistory.Max(move => move.MoveNumber);
        _readyPlayers.Clear();
        foreach (var readyId in room.ReadyPlayers) _readyPlayers.Add(readyId);
        var canMove = !_spectator && CanMove(room.CurrentTurnPlayerId) &&
            room.Status == "Playing" && !room.IsPaused;
        _view.DisplayGameBoard(_board, canMove);
        _view.DisplaySpectators(room.SpectatorCount);
        _view.DisplayReadyState(
            _state.Current.PlayerId is Guid playerId && _readyPlayers.Contains(playerId),
            room.Status == "Waiting" && !_spectator &&
                (_state.Current.PlayerId is not Guid current || !_readyPlayers.Contains(current)) && !_readyInFlight);
        _view.DisplayGameActionMode(ActionMode(room.Status));
        _view.DisplaySurrenderState(
            room.Status == "Playing" && !_spectator && !_surrenderInFlight);
        var currentPlayerReady = _state.Current.PlayerId is Guid currentId &&
            _readyPlayers.Contains(currentId);
        var turnMessage = room.Status switch
        {
            "Waiting" when _spectator => "Đang chờ người chơi sẵn sàng",
            "Waiting" when currentPlayerReady => "Đang chờ đối thủ sẵn sàng",
            "Waiting" => "Nhấn Sẵn sàng để bắt đầu",
            "Finished" => "Trận đấu đã kết thúc",
            "Cancelled" => "Phòng đã hủy",
            _ when room.IsPaused => "Trận đấu đang tạm dừng",
            _ when canMove => "Đến lượt bạn",
            _ => "Đang chờ đối thủ"
        };
        string activeSymbol = "";
        if (room.Status == "Playing" && !room.IsPaused)
        {
            activeSymbol = room.CurrentTurnPlayerId == _playerXId ? "X" : (room.CurrentTurnPlayerId == _playerOId ? "O" : "");
        }
        _view.DisplayGameTurn(turnMessage,
            room.Status == "Playing" ? room.TimeRemainingSec : 0,
            activeSymbol);
    }

    private bool CanMove(Guid? turnPlayerId) =>
        !_spectator && turnPlayerId is Guid turn && turn == _state.Current.PlayerId;

    private void UpdatePlayerNames(RoomSnapshot room)
    {
        _playerXId = room.PlayerXId;
        _playerOId = room.PlayerOId;
        _playerXName = ResolvePlayerName(room.PlayerXId, room.PlayerXName, "Người chơi X");
        _playerOName = ResolvePlayerName(room.PlayerOId, room.PlayerOName, "Người chơi O");
    }

    private string ResolvePlayerName(Guid playerId, string? suppliedName, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(suppliedName)) return suppliedName.Trim();
        if (_state.Current.PlayerId == playerId)
            return _state.Current.CurrentPlayer?.Nickname ?? "Bạn";
        return fallback;
    }

    private string PlayerName(Guid playerId) => playerId == _playerXId
        ? _playerXName
        : playerId == _playerOId
            ? _playerOName
            : "Người chơi";

    private string CurrentStatus() => _status;

    private GameActionMode ActionMode(string status)
    {
        if (_spectator) return GameActionMode.Spectator;

        return status switch
        {
            "Waiting" => GameActionMode.WaitingPlayer,
            "Playing" => GameActionMode.PlayingPlayer,
            "Finished" => GameActionMode.FinishedPlayer,
            _ => GameActionMode.Pending
        };
    }

    private static string?[,] ToBoard(string?[][] board)
    {
        // A newly created room is still in Waiting state and the server does
        // not allocate a match board until both players are ready.
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
        _view.ReadyRequested -= ReadyRequested;
        _view.RematchRequested -= RematchRequested;
        _view.SurrenderRequested -= SurrenderRequested;
        _view.MoveRequested -= MoveRequested;
        _view.LeaveRoomRequested -= LeaveRequested;
        _rooms.PlayerReady -= PlayerReady;
        _rooms.MatchStarted -= MatchStarted;
        _rooms.JoinedAsSpectator -= JoinedAsSpectator;
        _rooms.SpectatorLeft -= SpectatorLeft;
        _rooms.RoomCancelled -= RoomCancelled;
        _rooms.WaitingRoomUpdated -= WaitingRoomUpdated;
        _rooms.RematchOffered -= RematchOffered;
        _rooms.RematchResponded -= RematchResponded;
        _gameplay.MoveReceived -= MoveReceived;
        _gameplay.GameOverReceived -= GameOverReceived;
        _gameplay.BoardStateReceived -= BoardStateReceived;
        _gameplay.MatchPaused -= MatchPaused;
        _gameplay.MatchResumed -= MatchResumed;
        _gameplay.TurnTimedOut -= TurnTimedOut;
    }
}
