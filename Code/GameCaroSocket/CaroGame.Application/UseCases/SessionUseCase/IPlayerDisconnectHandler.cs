namespace CaroGame.Application.UseCases.SessionUseCase
{
    public interface IPlayerDisconnectHandler
    {
        Task HandleAsync(Guid playerId, CancellationToken cancellationToken);
    }
}

