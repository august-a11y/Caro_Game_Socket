using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;

namespace CaroGame.Application.UseCases.GamePlay
{
    public interface IMatchEnder
    {
        Room EndMatch(
            Guid roomId,
            MatchResultType matchResultType, string? reason = null);
    }
}

