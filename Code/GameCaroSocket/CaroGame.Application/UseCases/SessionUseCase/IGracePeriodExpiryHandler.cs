using CaroGame.Domain.Entities;

namespace CaroGame.Application.UseCases.SessionUseCase
{
    public interface IGracePeriodExpiryHandler
    {
        Task<Room> HandleAsync(Guid roomId, Guid playerId, CancellationToken cancellationToken);
    }
}

