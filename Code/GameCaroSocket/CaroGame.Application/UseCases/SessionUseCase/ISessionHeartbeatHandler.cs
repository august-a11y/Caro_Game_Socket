namespace CaroGame.Application.UseCases.SessionUseCase;

public interface ISessionHeartbeatHandler
{
    Task HandleAsync(Guid playerId, CancellationToken cancellationToken);
}
