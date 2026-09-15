using CaroGame.Domain.Entities;

namespace CaroGame.Application.UseCases.SessionUseCase
{
    public interface IGracePeriodExpiryHandler
    {
        Room Handle(Guid roomId, Guid playerId);
    }
}

