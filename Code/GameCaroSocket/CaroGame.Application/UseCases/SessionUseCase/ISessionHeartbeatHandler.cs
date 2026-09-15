namespace CaroGame.Application.UseCases.SessionUseCase;

public interface ISessionHeartbeatHandler
{
    void Handle(Guid playerId);
}
