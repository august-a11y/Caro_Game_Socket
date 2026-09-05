using CaroGame.Application.Interfaces.Repositories;

namespace CaroGame.Application.UseCases.SessionUseCase;

public sealed class SessionHeartbeatHandler : ISessionHeartbeatHandler
{
    private readonly ISessionRepository _sessionRepository;
    private readonly TimeProvider _timeProvider;

    public SessionHeartbeatHandler(
        ISessionRepository sessionRepository,
        TimeProvider timeProvider)
    {
        _sessionRepository = sessionRepository
            ?? throw new ArgumentNullException(nameof(sessionRepository));
        _timeProvider = timeProvider
            ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task HandleAsync(Guid playerId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var session = await _sessionRepository.GetByPlayerIdAsync(playerId);
        if (session is null || !session.IsConnected)
            return;

        cancellationToken.ThrowIfCancellationRequested();

        session.UpdateHeartbeat(_timeProvider.GetUtcNow().UtcDateTime);
        await _sessionRepository.UpdateAsync(session);
    }
}
