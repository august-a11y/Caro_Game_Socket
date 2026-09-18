using Caro.Client.WinForms.Core;
using Caro.Client.WinForms.Networking;
using CaroGame.Shared.Networking.Messaging;
using CaroGame.Shared.Protocol.Contracts;

namespace Caro.Client.WinForms.Services;

internal sealed class LobbyRequestException(ErrorResponse error) : InvalidOperationException(error.Message)
{
    public string ErrorCode { get; } = error.ErrorCode;
}

internal sealed class LobbyClientService : IDisposable
{
    private readonly ClientStateStore _state;
    private readonly ClientRequestManager _requests;
    private bool _disposed;

    public event Action<PlayerDto>? PlayerOnline;
    public event Action<Guid>? PlayerOffline;
    public event Action<PlayerDto>? PlayerStatusChanged;

    public LobbyClientService(ClientNetworkHost network, ClientStateStore state, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(network);
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _requests = new ClientRequestManager(
            network,
            timeout ?? TimeSpan.FromSeconds(10),
            error => new LobbyRequestException(error));

        network.Router.Register<PlayersResponse>(MessageTypes.OnlinePlayersListResponse, (response, _) =>
        {
            _requests.TryComplete(response.RequestId, MessageTypes.OnlinePlayersListResponse, response);
            return Task.CompletedTask;
        });
        network.Router.Register<MatchesResponse>(MessageTypes.ActiveMatchesListResponse, (response, _) =>
        {
            _requests.TryComplete(response.RequestId, MessageTypes.ActiveMatchesListResponse, response);
            return Task.CompletedTask;
        });
        network.Router.Register<PlayerDto>(MessageTypes.PlayerOnlineNotification, (player, _) =>
        {
            PlayerOnline?.Invoke(player);
            return Task.CompletedTask;
        });
        network.Router.Register<PlayerOfflineNotification>(MessageTypes.PlayerOfflineNotification, (notification, _) =>
        {
            PlayerOffline?.Invoke(notification.PlayerId);
            return Task.CompletedTask;
        });
        network.Router.Register<PlayerDto>(MessageTypes.PlayerStatusChangedNotification, (player, _) =>
        {
            PlayerStatusChanged?.Invoke(player);
            return Task.CompletedTask;
        });
    }

    public async Task<IReadOnlyList<PlayerDto>> GetOnlinePlayersAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        EnsureJoined();
        var response = await RequestAsync<PlayersResponse>(
            MessageTypes.OnlinePlayersListRequest,
            MessageTypes.OnlinePlayersListResponse,
            new RequestMessage { RequestId = Guid.NewGuid() }, cancellationToken);
        return response.Players.Where(player => player.Status != "Offline").DistinctBy(player => player.PlayerId)
            .OrderBy(player => player.Nickname, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(player => player.PlayerId).ToArray();
    }

    public async Task<IReadOnlyList<MatchSummaryDto>> GetActiveMatchesAsync(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        EnsureJoined();
        var response = await RequestAsync<MatchesResponse>(
            MessageTypes.ActiveMatchesListRequest,
            MessageTypes.ActiveMatchesListResponse,
            new RequestMessage { RequestId = Guid.NewGuid() }, cancellationToken);
        return response.Matches.Where(match => match.RoomId != Guid.Empty)
            .DistinctBy(match => match.RoomId)
            .OrderByDescending(match => match.StartedAt)
            .ThenBy(match => match.RoomId).ToArray();
    }

    private Task<TResponse> RequestAsync<TResponse>(MessageTypes requestType,
        MessageTypes responseType, RequestMessage request, CancellationToken cancellationToken) =>
        _requests.SendAsync<RequestMessage, TResponse>(
            requestType, new[] { responseType }, request, cancellationToken);

    private void EnsureJoined()
    {
        if (!_state.Current.IsJoined)
            throw new InvalidOperationException("Hãy kết nối máy chủ trước khi sử dụng sảnh.");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _requests.Dispose();
    }
}
