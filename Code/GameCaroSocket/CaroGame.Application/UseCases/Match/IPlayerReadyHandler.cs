using CaroGame.Domain.Entities;

namespace CaroGame.Application.UseCases.Match;

public interface IPlayerReadyHandler
{
    Room Handle(
        Guid roomId,
        Guid playerId);
}
