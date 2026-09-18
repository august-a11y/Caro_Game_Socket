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

    public void Handle(Guid playerId)
    {
        var session = _sessionRepository.GetByPlayerId(playerId);
        if (session is null || !session.IsConnected)
            return;


        session.UpdateHeartbeat(_timeProvider.GetUtcNow().UtcDateTime);
        _sessionRepository.Update(session);
    }
}
