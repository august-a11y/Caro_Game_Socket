namespace CaroGame.Application.UseCases.SessionUseCase
{
    public interface IPlayerDisconnectHandler
    {
        void Handle(Guid playerId);
    }
}

