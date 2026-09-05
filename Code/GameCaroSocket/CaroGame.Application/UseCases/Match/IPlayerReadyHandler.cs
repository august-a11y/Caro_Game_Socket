using CaroGame.Domain.Entities;

namespace CaroGame.Application.UseCases.Match;

public interface IPlayerReadyHandler
{
    Task<Room> HandleAsync(
        Guid roomId,
        Guid playerId,
        CancellationToken cancellationToken = default);
}
