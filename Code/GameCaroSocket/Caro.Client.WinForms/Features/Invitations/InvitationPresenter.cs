using Caro.Client.WinForms.Core;
using Caro.Client.WinForms.Services;
using CaroGame.Shared.Protocol.Contracts;

namespace Caro.Client.WinForms.Features.Invitations;

internal sealed class InvitationPresenter : IDisposable
{
    private readonly InvitationsView _view;
    private readonly MatchmakingClientService _service;
    private readonly ClientStateStore _state;
    private readonly ClientLogger _logger = ClientLogger.Shared;
    private readonly object _sync = new();
    private readonly HashSet<Guid> _pending = new();
    private readonly Dictionary<Guid, ChallengeResponse> _challenges = new();
    private bool _disposed;

    public event Action<int>? PendingCountChanged;
    public event Action? ChallengeAcceptStarted;
    public event Action? ChallengeAcceptFailed;

    public InvitationPresenter(InvitationsView view, MatchmakingClientService service,
        ClientStateStore state)
    {
        _view = view ?? throw new ArgumentNullException(nameof(view));
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _view.RefreshRequested += RefreshRequested;
        _view.ChallengeResponseRequested += RespondRequested;
        _view.ChallengeCancelRequested += CancelRequested;
        _service.ChallengeReceived += ChallengeReceived;
        _service.ChallengeAccepted += ChallengeUpdated;
        _service.ChallengeDeclined += ChallengeUpdated;
        _service.ChallengeExpired += ChallengeUpdated;
        _service.ChallengeCancelled += ChallengeUpdated;
    }

    public void Refresh() => Render();

    private void RefreshRequested() => Render();

    public void Track(ChallengeResponse response)
    {
        lock (_sync) _challenges[response.Challenge.ChallengeId] = response;
        Render();
    }

    private void ChallengeReceived(ChallengeResponse response)
    {
        lock (_sync) _challenges[response.Challenge.ChallengeId] = response;
        Render();
    }

    private void ChallengeUpdated(ChallengeResponse response)
    {
        lock (_sync) _challenges[response.Challenge.ChallengeId] = response;
        Render();
    }

    private async void RespondRequested(Guid challengeId, bool accept)
    {
        lock (_sync)
        {
            if (!_pending.Add(challengeId)) return;
        }
        Render();
        _logger.Info($"Challenge response requested: challenge={challengeId}, accept={accept}");
        if (accept) ChallengeAcceptStarted?.Invoke();
        _view.DisplayStatus(accept ? "Đang chấp nhận lời mời..." : "Đang từ chối lời mời...");
        try
        {
            var response = await _service.RespondToChallengeAsync(challengeId, accept);
            lock (_sync) _challenges[challengeId] = response;
            _logger.Info($"Challenge response received: challenge={challengeId}, status={response.Challenge.Status}, room={response.Room?.RoomId}");
            if (accept && (response.Challenge.Status != "Accepted" || response.Room is null))
                ChallengeAcceptFailed?.Invoke();
            Render();
        }
        catch (OperationCanceledException) when (_disposed) { }
        catch (Exception error)
        {
            if (!_disposed)
            {
                _logger.Error($"Challenge response failed: challenge={challengeId}", error);
                if (accept) ChallengeAcceptFailed?.Invoke();
                _view.DisplayError(error.Message);
            }
        }
        finally
        {
            lock (_sync) _pending.Remove(challengeId);
            Render();
        }
    }

    private async void CancelRequested(Guid challengeId)
    {
        lock (_sync)
        {
            if (!_pending.Add(challengeId)) return;
        }
        Render();
        _view.DisplayStatus("Đang hủy lời mời...");
        try
        {
            var response = await _service.CancelChallengeAsync(challengeId);
            lock (_sync) _challenges[challengeId] = response;
            Render();
        }
        catch (OperationCanceledException) when (_disposed) { }
        catch (Exception error)
        {
            if (!_disposed) _view.DisplayError(error.Message);
        }
        finally
        {
            lock (_sync) _pending.Remove(challengeId);
            Render();
        }
    }

    private void Render()
    {
        var playerId = _state.Current.PlayerId;
        if (playerId is not Guid current) return;
        ChallengeResponse[] snapshot;
        HashSet<Guid> pendingCommands;
        lock (_sync)
        {
            snapshot = _challenges.Values.ToArray();
            pendingCommands = [.. _pending];
        }
        PendingCountChanged?.Invoke(snapshot.Count(response =>
            response.Challenge.ToPlayerId == current && response.Challenge.Status == "Pending"));
        _view.DisplayInvitations(snapshot.Select(response =>
            new InvitationRow(
                response.Challenge.ChallengeId,
                PeerName(response.Challenge, current),
                response.Challenge.ExpiresAt,
                UiText.ChallengeStatus(response.Challenge.Status),
                response.Challenge.ToPlayerId == current,
                response.Challenge.Status == "Pending",
                pendingCommands.Contains(response.Challenge.ChallengeId))));
    }

    private static string PeerName(ChallengeDto challenge, Guid currentPlayerId)
    {
        var name = challenge.FromPlayerId == currentPlayerId
            ? challenge.ToPlayerName
            : challenge.FromPlayerName;
        return string.IsNullOrWhiteSpace(name) ? "Người chơi" : name.Trim();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _view.RefreshRequested -= RefreshRequested;
        _view.ChallengeResponseRequested -= RespondRequested;
        _view.ChallengeCancelRequested -= CancelRequested;
        _service.ChallengeReceived -= ChallengeReceived;
        _service.ChallengeAccepted -= ChallengeUpdated;
        _service.ChallengeDeclined -= ChallengeUpdated;
        _service.ChallengeExpired -= ChallengeUpdated;
        _service.ChallengeCancelled -= ChallengeUpdated;
    }
}
