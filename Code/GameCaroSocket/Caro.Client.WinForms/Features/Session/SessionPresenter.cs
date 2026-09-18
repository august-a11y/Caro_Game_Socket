using Caro.Client.WinForms.Core;
using Caro.Client.WinForms.Networking;
using Caro.Client.WinForms.Services;
using CaroGame.Shared.Protocol.Contracts;

namespace Caro.Client.WinForms.Features.Session;

internal sealed class SessionPresenter
{
    private readonly FMain _view;
    private readonly ClientNetworkHost _network = new();
    private readonly ClientStateStore _state = new();
    private readonly SessionCache _cache = new();
    private readonly SessionClientService _session;
    private readonly LobbyClientService _lobby;
    private readonly MatchmakingClientService _matchmaking;
    private readonly RoomClientService _rooms;
    private readonly GameplayClientService _gameplay;
    private readonly Caro.Client.WinForms.Features.OnlinePlayers.OnlinePlayerPresenter _onlinePlayers;
    private readonly Caro.Client.WinForms.Features.Matches.MatchesPresenter _matches;
    private readonly Caro.Client.WinForms.Features.Invitations.InvitationPresenter _invitations;
    private readonly Caro.Client.WinForms.Features.Game.GamePresenter _game;
    private readonly CancellationTokenSource _lifetime = new();
    private string _host = "";
    private int _port = 5000;
    private bool _busy;
    private bool _closing;
    private bool _closed;
    private int _challengeInFlight;
    private Task _command = Task.CompletedTask;
    public bool LastConnectionCancelled { get; private set; }

    public SessionPresenter(FMain view)
    {
        _view = view;
        if (_cache.Load() is { } saved)
        {
            _host = saved.Host;
            _port = saved.Port;
            _state.Update(_ => saved.ToDisconnectedState());
        }
        _session = new SessionClientService(_network, _state);
        _lobby = new LobbyClientService(_network, _state);
        _matchmaking = new MatchmakingClientService(_network, _state);
        _rooms = new RoomClientService(_network, _state);
        _gameplay = new GameplayClientService(_network, _state);
        _onlinePlayers = new Caro.Client.WinForms.Features.OnlinePlayers.OnlinePlayerPresenter(view, _lobby, _state, _network.Connection);
        _matches = new Caro.Client.WinForms.Features.Matches.MatchesPresenter(view, _lobby, _state, _rooms);
        _invitations = new Caro.Client.WinForms.Features.Invitations.InvitationPresenter(view.InvitationsPage, _matchmaking, _state);
        _game = new Caro.Client.WinForms.Features.Game.GamePresenter(view, _rooms, _gameplay, _state);
        _view.InvitationsRequested += _invitations.Refresh;
        _view.ConnectRequested += ReconnectRequested;
        _view.DisconnectRequested += DisconnectRequested;
        _invitations.PendingCountChanged += _view.DisplayInvitationCount;
        _invitations.ChallengeAcceptStarted += _view.OpenPendingRoom;
        _invitations.ChallengeAcceptFailed += _view.ClosePendingRoom;
        _view.ChallengeRequested += ChallengeRequested;
        _matchmaking.ChallengeAccepted += ChallengeAccepted;
        _view.FormClosing += FormClosing;
        _network.Connection.Disconnected += Disconnected;
        _network.Router.HandlerFailed += MessageHandlingFailed;
        _session.HeartbeatFailed += error => _view.DisplayStatus(
            $"Mất kết nối với máy chủ: {error.Message} Hãy kết nối lại.");
    }

    private async void ChallengeRequested(Guid opponentId)
    {
        if (Interlocked.Exchange(ref _challengeInFlight, 1) != 0)
        {
            _view.DisplayStatus("Đang gửi một lời thách đấu khác.");
            return;
        }
        _network.Logger.Info($"Challenge send requested: opponent={opponentId}");
        try
        {
            var response = await _matchmaking.SendChallengeAsync(opponentId, _lifetime.Token);
            _invitations.Track(response);
            _network.Logger.Info($"Challenge sent: challenge={response.Challenge.ChallengeId}");
            _view.DisplayStatus("Đã gửi lời thách đấu.");
        }
        catch (OperationCanceledException) when (_closing) { }
        catch (Exception error)
        {
            if (!_closing)
            {
                _network.Logger.Error("Challenge send failed.", error);
                _view.DisplayStatus(error.Message);
            }
        }
        finally { Interlocked.Exchange(ref _challengeInFlight, 0); }
    }

    private void ChallengeAccepted(ChallengeResponse response)
    {
        _network.Logger.Info($"Challenge accepted: challenge={response.Challenge.ChallengeId}, room={response.Room?.RoomId}");
        if (response.Room is not null)
            _game.Open(response.Room);
        else
        {
            _view.ClosePendingRoom();
            _view.DisplayStatus("Máy chủ đã chấp nhận lời mời nhưng không trả về thông tin phòng.");
        }
    }

    public async Task<bool> ConnectAtStartupAsync()
    {
        if (_closing) return false;
        return await ShowConnectionAsync();
    }

    private async void ReconnectRequested()
    {
        try { await ShowConnectionAsync(); }
        catch (Exception error) when (!_closing)
        {
            _network.Logger.Error("Reconnect UI flow failed.", error);
            _view.DisplayStatus(error.Message);
        }
    }

    private async void DisconnectRequested()
    {
        if (_busy || _closing) return;
        _busy = true;
        _view.DisplaySessionBusy(true);
        try { await _network.Connection.DisconnectAsync(); }
        catch (Exception error) { _view.DisplayStatus(error.Message); }
        finally
        {
            _busy = false;
            if (!_closing) _view.DisplaySessionBusy(false);
        }
    }

    private async Task<bool> ShowConnectionAsync()
    {
        if (_busy || _closing) return false;
        LastConnectionCancelled = false;
        var state = _state.Current;
        using var dialog = new ConnectionDialog(_network.Discovery, _host, _port, state.CurrentPlayer?.Nickname ?? "",
            state.PlayerId.HasValue && state.SessionId.HasValue);
        dialog.ConnectionRequested += (server, nickname, reconnect) =>
            ConnectAsync(dialog, state, server, nickname, reconnect);
        _busy = true;
        _view.DisplaySessionBusy(true);
        try
        {
            DialogResult result;
            try { result = dialog.ShowDialog(_view); }
            finally { await _network.Discovery.StopAsync(); }
            if (result != DialogResult.OK || _closing)
            {
                LastConnectionCancelled = result != DialogResult.OK;
                return false;
            }
            return _state.Current.IsJoined;
        }
        finally
        {
            _busy = false;
            if (!_closing) _view.DisplaySessionBusy(false);
        }
    }

    private async Task<bool> ConnectAsync(ConnectionDialog dialog, ClientState previous,
        CaroGame.Shared.Networking.ServerInfo server, string nickname, bool reconnect)
    {
        try
        {
            _view.DisplayStatus("Đang kết nối máy chủ...");
            using var connectTimeout = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
            connectTimeout.CancelAfter(TimeSpan.FromSeconds(10));
            await _network.Connection.DisconnectAsync();
            string host = server.Host;
            int port = server.TcpPort;
            await _network.ConnectAsync(host, port, connectTimeout.Token);
            var response = reconnect
                ? await _session.ReconnectAsync(previous.PlayerId!.Value, previous.SessionId!.Value, _lifetime.Token)
                : await _session.JoinAsync(nickname, _lifetime.Token);
            _host = host;
            _port = port;
            string? cacheWarning = null;
            try
            {
                _cache.Save(new SavedSession(host, port, response.PlayerId, response.SessionId, response.Player.Nickname));
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                cacheWarning = " Không thể lưu phiên để kết nối lại sau khi đóng ứng dụng.";
            }
            _view.DisplayConnection(true, response.Player.Nickname,
                string.IsNullOrWhiteSpace(server.ServerName) ? "Máy chủ Caro" : server.ServerName.Trim());
            RestoreRoom(response);
            _view.DisplayStatus((reconnect ? "Đã khôi phục phiên kết nối." : "Đã tham gia máy chủ.") + cacheWarning);
            await _onlinePlayers.RefreshAsync();
            return true;
        }
        catch (Exception error)
        {
            await _network.Connection.DisconnectAsync();
            bool expiredSession = dialog.Reconnect && error is SessionRequestException { ErrorCode: "Unauthorized" or "NotFound" };
            if (expiredSession)
            {
                _state.Update(_ => new ClientState());
                try { _cache.Clear(); }
                catch (Exception cacheError) when (cacheError is IOException or UnauthorizedAccessException) { }
            }
            if (!_closing)
            {
                _view.DisplayConnection(false);
                dialog.DisplayConnectionError(expiredSession
                    ? "Phiên cũ đã hết hạn hoặc máy chủ đã khởi động lại. Hãy bỏ chọn Reconnect để tạo phiên mới."
                    : error is OperationCanceledException
                    ? "Đã bị hủy hoặc quá thời gian chờ."
                    : error.Message);
                _view.DisplayStatus(expiredSession
                    ? "Phiên cũ đã hết hạn hoặc máy chủ đã khởi động lại. Hãy kết nối lại để tạo phiên mới."
                    : error is OperationCanceledException
                    ? "Kết nối đã bị hủy hoặc quá thời gian chờ."
                    : $"Không thể kết nối: {error.Message}");
            }
            return false;
        }
    }

    private void RestoreRoom(SessionResponse response)
    {
        if (response.Room is { } room)
        {
            _game.Open(room);
            _view.DisplayGameHint(room.ClosingReason is { Length: > 0 }
                ? UiText.ClosingReason(room.ClosingReason)
                : "Đã khôi phục trạng thái phòng từ máy chủ.");
            return;
        }

        if (response.LastRoom is { } lastRoom)
        {
            var summary = lastRoom.Result is { Length: > 0 }
                ? UiText.MatchResult(lastRoom.Result)
                : lastRoom.ClosingReason is { Length: > 0 }
                    ? UiText.ClosingReason(lastRoom.ClosingReason)
                    : UiText.RoomStatus(lastRoom.Status);
            _view.DisplayStatus($"Ván gần nhất: {summary}.");
        }
    }

    private void Disconnected()
    {
        _view.DisplayConnection(false);
        if (!_closing) _view.DisplayStatus("Đã ngắt kết nối. Có thể kết nối lại bằng phiên trước.");
    }

    private void MessageHandlingFailed(CaroGame.Shared.Networking.Messaging.MessageTypes type, Exception error)
    {
        _view.DisplayStatus($"Không thể xử lý dữ liệu {type}: {error.Message}");
    }

    private async void FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_closed) return;
        e.Cancel = true;
        if (_closing) return;
        _closing = true;
        _lifetime.Cancel();
        try
        {
            await _command;
            _view.InvitationsRequested -= _invitations.Refresh;
            _view.ConnectRequested -= ReconnectRequested;
            _view.DisconnectRequested -= DisconnectRequested;
            _invitations.PendingCountChanged -= _view.DisplayInvitationCount;
            _invitations.ChallengeAcceptStarted -= _view.OpenPendingRoom;
            _invitations.ChallengeAcceptFailed -= _view.ClosePendingRoom;
            _view.ChallengeRequested -= ChallengeRequested;
            _matchmaking.ChallengeAccepted -= ChallengeAccepted;
            _game.Dispose();
            _invitations.Dispose();
            _matches.Dispose();
            _onlinePlayers.Dispose();
            _gameplay.Dispose();
            _rooms.Dispose();
            _matchmaking.Dispose();
            _lobby.Dispose();
            await _session.DisposeAsync();
            await _network.DisposeAsync();
        }
        finally
        {
            _network.Connection.Disconnected -= Disconnected;
            _network.Router.HandlerFailed -= MessageHandlingFailed;
            _lifetime.Dispose();
            _closed = true;
            _view.Close();
        }
    }
}
