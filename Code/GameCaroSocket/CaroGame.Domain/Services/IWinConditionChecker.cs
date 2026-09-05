using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;
using CaroGame.Domain.ValueObjects;

namespace CaroGame.Domain.Services
{
    public interface IWinConditionChecker
    {
        MatchResultType Check(Board board, Move lastMove);  
    }
}
