using Caro.Client.WinForms.Core;
using Caro.Client.WinForms.Networking;
using CaroGame.Shared.Networking.Messaging;
using CaroGame.Shared.Protocol.Contracts;

namespace Caro.Client.WinForms.Services;

internal sealed class MatchmakingRequestException(ErrorResponse error) : InvalidOperationException(error.Message)
{
    public string ErrorCode { get; } = error.ErrorCode;
}

internal sealed class MatchmakingClientService : IDisposable
{
    private readonly ClientNetworkHost _network;
    private readonly ClientStateStore _state;
    private readonly ClientRequestManager _requests;
    private bool _disposed;

    public event Action<ChallengeResponse>? ChallengeReceived;
    public event Action<ChallengeResponse>? ChallengeAccepted;
    public event Action<ChallengeResponse>? ChallengeDeclined;
    public event Action<ChallengeResponse>? ChallengeExpired;
    public event Action<ChallengeResponse>? ChallengeCancelled;

    public MatchmakingClientService(ClientNetworkHost network, ClientStateStore state, TimeSpan? timeout = null)
    {
        _network = network ?? throw new ArgumentNullException(nameof(network));
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _requests = new ClientRequestManager(
            network,
            timeout ?? TimeSpan.FromSeconds(10),
            error => new MatchmakingRequestException(error));

        RegisterChallengeHandler(MessageTypes.ChallengeReceivedNotification,
            response => ChallengeReceived?.Invoke(response));
        RegisterChallengeHandler(MessageTypes.ChallengeAcceptedNotification,
            response => ChallengeAccepted?.Invoke(response));
        RegisterChallengeHandler(MessageTypes.ChallengeDeclinedNotification,
            response => ChallengeDeclined?.Invoke(response));
        RegisterChallengeHandler(MessageTypes.ChallengeExpiredNotification,
            response => ChallengeExpired?.Invoke(response));
        RegisterChallengeHandler(MessageTypes.ChallengeCancelledNotification,
            response => ChallengeCancelled?.Invoke(response));
        network.Router.Register<ChallengeResponse>(MessageTypes.ChallengeSendResponse,
            (response, _) =>
            {
                _requests.TryComplete(response.RequestId, MessageTypes.ChallengeSendResponse, response);
                return Task.CompletedTask;
            });
    }

    public Task<ChallengeResponse> SendChallengeAsync(Guid opponentId,
        CancellationToken cancellationToken = default)
    {
        EnsureJoined();
        if (opponentId == Guid.Empty)
            throw new ArgumentException("OpponentId không được rỗng.", nameof(opponentId));
        return RequestAsync(
            MessageTypes.ChallengeRequest,
            new ChallengeRequest { RequestId = Guid.NewGuid(), OpponentId = opponentId },
            new[] { MessageTypes.ChallengeSendResponse }, cancellationToken);
    }

    public Task<ChallengeResponse> RespondToChallengeAsync(Guid challengeId, bool accept,
        CancellationToken cancellationToken = default)
    {
        EnsureJoined();
        ValidateChallengeId(challengeId);
        return RequestAsync(
            MessageTypes.ChallengeRespondRequest,
            new ChallengeRespondRequest
            {
                RequestId = Guid.NewGuid(), ChallengeId = challengeId, Accept = accept
            },
            new[]
            {
                MessageTypes.ChallengeAcceptedNotification,
                MessageTypes.ChallengeDeclinedNotification,
                MessageTypes.ChallengeExpiredNotification
            }, cancellationToken);
    }

    public Task<ChallengeResponse> CancelChallengeAsync(Guid challengeId,
        CancellationToken cancellationToken = default)
    {
        EnsureJoined();
        ValidateChallengeId(challengeId);
        return RequestAsync(
            MessageTypes.ChallengeCancelRequest,
            new ChallengeReferenceRequest
            {
                RequestId = Guid.NewGuid(), ChallengeId = challengeId
            },
            new[]
            {
                MessageTypes.ChallengeCancelledNotification,
                MessageTypes.ChallengeExpiredNotification
            }, cancellationToken);
    }

    private void RegisterChallengeHandler(MessageTypes type, Action<ChallengeResponse> notify)
    {
        _network.Router.Register<ChallengeResponse>(type, (response, _) =>
        {
            _requests.TryComplete(response.RequestId, type, response);
            notify(response);
            return Task.CompletedTask;
        });
    }

    private Task<ChallengeResponse> RequestAsync<TRequest>(MessageTypes requestType,
        TRequest request, IReadOnlyCollection<MessageTypes> responseTypes,
        CancellationToken cancellationToken) where TRequest : RequestMessage =>
        _requests.SendAsync<TRequest, ChallengeResponse>(
            requestType, responseTypes, request, cancellationToken);

    private void EnsureJoined()
    {
        if (!_state.Current.IsJoined)
            throw new InvalidOperationException("Hãy Join hoặc Reconnect trước khi thách đấu.");
    }

    private static void ValidateChallengeId(Guid challengeId)
    {
        if (challengeId == Guid.Empty)
            throw new ArgumentException("ChallengeId không được rỗng.", nameof(challengeId));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _requests.Dispose();
    }
}
