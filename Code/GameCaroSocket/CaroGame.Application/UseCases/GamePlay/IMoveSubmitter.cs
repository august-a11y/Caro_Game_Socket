using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;
using CaroGame.Domain.ValueObjects;

namespace CaroGame.Application.UseCases.GamePlay
{
    public interface IMoveSubmitter
    {
        MatchResultType SubmitMove(Guid roomId, Guid playerId, Position position);
    }
}

