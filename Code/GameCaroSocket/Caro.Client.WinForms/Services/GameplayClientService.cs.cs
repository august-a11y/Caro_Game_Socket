using Caro.Client.WinForms.Core;
using Caro.Client.WinForms.Networking;
using CaroGame.Shared.Networking.Messaging;
using CaroGame.Shared.Protocol.Contracts;

namespace Caro.Client.WinForms.Services;

internal sealed class GameplayRequestException(ErrorResponse error) : InvalidOperationException(error.Message)
{
    public string ErrorCode { get; } = error.ErrorCode;
}

internal sealed class GameplayClientService : IDisposable
{
    private readonly ClientNetworkHost _network;
    private readonly ClientStateStore _state;
    private readonly ClientRequestManager _requests;
    private bool _disposed;

    public event Action<MoveNotification>? MoveReceived;
    public event Action<GameOverNotification>? GameOverReceived;
    public event Action<RoomSnapshot>? BoardStateReceived;
    public event Action<MatchPausedNotification>? MatchPaused;
    public event Action<RoomSnapshot>? MatchResumed;
    public event Action<TurnTimeoutNotification>? TurnTimedOut;

    public GameplayClientService(ClientNetworkHost network, ClientStateStore state, TimeSpan? timeout = null)
    {
        _network = network ?? throw new ArgumentNullException(nameof(network));
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _requests = new ClientRequestManager(
            network,
            timeout ?? TimeSpan.FromSeconds(10),
            error => new GameplayRequestException(error));

        Register(MessageTypes.MoveBroadcastNotification, (MoveNotification notification) =>
        {
            _requests.TryComplete(notification.RequestId, MessageTypes.MoveBroadcastNotification, notification);
            MoveReceived?.Invoke(notification);
        });
        Register(MessageTypes.GameOverNotification, (GameOverNotification notification) =>
        {
            _requests.TryComplete(notification.RequestId, MessageTypes.GameOverNotification, notification);
            GameOverReceived?.Invoke(notification);
        });
        Register(MessageTypes.BoardStateSnapshotResponse, (RoomResponse response) =>
        {
            _requests.TryComplete(response.RequestId, MessageTypes.BoardStateSnapshotResponse, response);
            BoardStateReceived?.Invoke(response.Room);
        });
        Register(MessageTypes.MatchPausedNotification, (MatchPausedNotification notification) =>
        {
            MatchPaused?.Invoke(notification);
        });
        Register(MessageTypes.MatchResumedNotification, (RoomResponse response) =>
        {
            MatchResumed?.Invoke(response.Room);
        });
        Register(MessageTypes.TurnTimeoutNotification, (TurnTimeoutNotification notification) =>
        {
            TurnTimedOut?.Invoke(notification);
        });
    }

    public Task<MoveNotification> SubmitMoveAsync(Guid roomId, int row, int column,
        CancellationToken cancellationToken = default)
    {
        EnsureJoined();
        ValidateRoomId(roomId);
        if (row is < 0 or >= 15)
            throw new ArgumentOutOfRangeException(nameof(row), "Row phải nằm trong khoảng 0 đến 14.");
        if (column is < 0 or >= 15)
            throw new ArgumentOutOfRangeException(nameof(column), "Column phải nằm trong khoảng 0 đến 14.");

        return _requests.SendAsync<SubmitMoveRequest, MoveNotification>(
            MessageTypes.MoveRequest,
            new[] { MessageTypes.MoveBroadcastNotification },
            new SubmitMoveRequest
            {
                RequestId = Guid.NewGuid(), RoomId = roomId, Row = row, Column = column
            }, cancellationToken);
    }

    public Task<GameOverNotification> SurrenderAsync(Guid roomId,
        CancellationToken cancellationToken = default)
    {
        EnsureJoined();
        ValidateRoomId(roomId);
        return _requests.SendAsync<RoomRequest, GameOverNotification>(
            MessageTypes.SurrenderRequest,
            new[] { MessageTypes.GameOverNotification },
            new RoomRequest { RequestId = Guid.NewGuid(), RoomId = roomId },
            cancellationToken);
    }

    public async Task<RoomSnapshot> GetBoardStateAsync(Guid roomId,
        CancellationToken cancellationToken = default)
    {
        EnsureJoined();
        ValidateRoomId(roomId);
        var response = await _requests.SendAsync<RoomRequest, RoomResponse>(
            MessageTypes.BoardStateSnapshotRequest,
            new[] { MessageTypes.BoardStateSnapshotResponse },
            new RoomRequest { RequestId = Guid.NewGuid(), RoomId = roomId },
            cancellationToken);
        return response.Room;
    }

    private void Register<TPayload>(MessageTypes type, Action<TPayload> handler)
    {
        _network.Router.Register<TPayload>(type, (payload, _) =>
        {
            handler(payload);
            return Task.CompletedTask;
        });
    }

    private void EnsureJoined()
    {
        if (!_state.Current.IsJoined)
            throw new InvalidOperationException("Hãy Join hoặc Reconnect trước khi thao tác với trận đấu.");
    }

    private static void ValidateRoomId(Guid roomId)
    {
        if (roomId == Guid.Empty)
            throw new ArgumentException("RoomId không được rỗng.", nameof(roomId));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _requests.Dispose();
    }
}
