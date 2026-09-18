using Caro.Client.WinForms.Core;
using Caro.Client.WinForms.Features.Room;
using Caro.Client.WinForms.Networking;
using CaroGame.Shared.Networking.Messaging;
using CaroGame.Shared.Protocol.Contracts;

namespace Caro.Client.WinForms.Services;

internal sealed class RoomRequestException(ErrorResponse error) : InvalidOperationException(error.Message)
{
    public string ErrorCode { get; } = error.ErrorCode;
}

internal sealed class RoomClientService : IRoomClientService, IDisposable
{
    private readonly ClientNetworkHost _network;
    private readonly ClientStateStore _state;
    private readonly ClientRequestManager _requests;
    private bool _disposed;

    public event Action<RoomPlayerNotification>? PlayerReady;
    public event Action<RoomSnapshot>? MatchStarted;
    public event Action<RoomPlayerNotification>? SpectatorJoined;
    public event Action<RoomPlayerNotification>? SpectatorLeft;
    public event Action<RoomSnapshot>? JoinedAsSpectator;
    public event Action<RoomCancelledNotification>? RoomCancelled;
    public event Action<RoomSnapshot>? WaitingRoomUpdated;
    public event Action<RematchOfferNotification>? RematchOffered;
    public event Action<RematchResponseNotification>? RematchResponded;

    public RoomClientService(ClientNetworkHost network, ClientStateStore state, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(network);
        _network = network;
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _requests = new ClientRequestManager(
            network,
            timeout ?? TimeSpan.FromSeconds(10),
            error => new RoomRequestException(error));

        Register(MessageTypes.PlayerReadyNotification, (RoomPlayerNotification notification) =>
        {
            _requests.TryComplete(notification.RequestId, MessageTypes.PlayerReadyNotification, notification);
            PlayerReady?.Invoke(notification);
        });
        Register(MessageTypes.MatchStartedNotification, (RoomResponse response) =>
        {
            _requests.TryComplete(response.RequestId, MessageTypes.MatchStartedNotification, response);
            MatchStarted?.Invoke(response.Room);
        });
        Register(MessageTypes.SpectatorJoinedNotification, (RoomPlayerNotification notification) =>
        {
            _requests.TryComplete(notification.RequestId, MessageTypes.SpectatorJoinedNotification, notification);
            SpectatorJoined?.Invoke(notification);
        });
        Register(MessageTypes.SpectatorLeftNotification, (RoomPlayerNotification notification) =>
        {
            _requests.TryComplete(notification.RequestId, MessageTypes.SpectatorLeftNotification, notification);
            SpectatorLeft?.Invoke(notification);
        });
        Register(MessageTypes.RoomCancelledNotification, (RoomCancelledNotification notification) =>
        {
            _requests.TryComplete(notification.RequestId, MessageTypes.RoomCancelledNotification, notification);
            RoomCancelled?.Invoke(notification);
        });
        Register(MessageTypes.WaitingRoomUpdatedNotification, (RoomResponse response) =>
        {
            _requests.TryComplete(response.RequestId, MessageTypes.WaitingRoomUpdatedNotification, response);
            WaitingRoomUpdated?.Invoke(response.Room);
        });
        Register(MessageTypes.RematchOfferNotification, (RematchOfferNotification notification) =>
        {
            RematchOffered?.Invoke(notification);
        });
        Register(MessageTypes.RematchResponseNotification, (RematchResponseNotification notification) =>
        {
            _requests.TryComplete(notification.RequestId, MessageTypes.RematchResponseNotification, notification);
            RematchResponded?.Invoke(notification);
        });
        Register(MessageTypes.JoinRoomAsSpectatorResponse, (RoomResponse response) =>
        {
            _requests.TryComplete(response.RequestId, MessageTypes.JoinRoomAsSpectatorResponse, response);
            JoinedAsSpectator?.Invoke(response.Room);
        });
    }

    public Task ReadyAsync(Guid roomId, CancellationToken cancellationToken = default) =>
        SendRoomCommandAsync(
            MessageTypes.PlayerReadyRequest,
            roomId,
            new[] { MessageTypes.PlayerReadyNotification, MessageTypes.RoomCancelledNotification },
            cancellationToken);

    public Task LeaveWaitingRoomAsync(Guid roomId, CancellationToken cancellationToken = default) =>
        SendRoomCommandAsync(
            MessageTypes.LeaveWaitingRoomRequest,
            roomId,
            new[] { MessageTypes.RoomCancelledNotification },
            cancellationToken);

    public Task JoinAsSpectatorAsync(Guid roomId, CancellationToken cancellationToken = default) =>
        SendRoomCommandAsync(
            MessageTypes.JoinRoomAsSpectatorRequest,
            roomId,
            new[] { MessageTypes.JoinRoomAsSpectatorResponse },
            cancellationToken);

    public Task LeaveAsSpectatorAsync(Guid roomId, CancellationToken cancellationToken = default) =>
        SendRoomCommandAsync(
            MessageTypes.LeaveRoomAsSpectatorRequest,
            roomId,
            new[] { MessageTypes.SpectatorLeftNotification },
            cancellationToken);

    public Task<RematchResponseNotification> RespondToRematchAsync(Guid roomId, bool accept,
        CancellationToken cancellationToken = default)
    {
        EnsureJoined();
        ValidateRoomId(roomId);
        return _requests.SendAsync<RematchResponseRequest, RematchResponseNotification>(
            MessageTypes.RematchResponseRequest,
            new[] { MessageTypes.RematchResponseNotification },
            new RematchResponseRequest
            {
                RequestId = Guid.NewGuid(), RoomId = roomId, Accept = accept
            }, cancellationToken);
    }

    private async Task SendRoomCommandAsync(MessageTypes requestType, Guid roomId,
        IReadOnlyCollection<MessageTypes> responseTypes, CancellationToken cancellationToken)
    {
        EnsureJoined();
        ValidateRoomId(roomId);
        await _requests.SendAsync<RoomRequest, object>(
            requestType,
            responseTypes,
            new RoomRequest { RequestId = Guid.NewGuid(), RoomId = roomId },
            cancellationToken);
    }

    private void Register<TPayload>(MessageTypes type, Action<TPayload> handler)
    {
        // MessageRouter owns one handler per type; this service owns room messages.
        _network.Router.Register<TPayload>(type, (payload, _) =>
        {
            handler(payload);
            return Task.CompletedTask;
        });
    }

    private void EnsureJoined()
    {
        if (!_state.Current.IsJoined)
            throw new InvalidOperationException("Hãy Join hoặc Reconnect trước khi thao tác với phòng.");
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
