using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;
using CaroGame.Domain.ValueObjects;

namespace CaroGame.Domain.Services;

public sealed class WinConditionChecker : IWinConditionChecker
{
    public MatchResultType Check(Board board, Move lastMove)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(lastMove);

        if (board.GetSymbol(lastMove.Position) != lastMove.Symbol)
            throw new ArgumentException("Last move does not match the board state.", nameof(lastMove));

        if (HasWon(board, lastMove.Position, lastMove.Symbol))
            return lastMove.Symbol == Symbol.X
                ? MatchResultType.PlayerXWin
                : MatchResultType.PlayerOWin;

        return board.IsFull() ? MatchResultType.Draw : MatchResultType.Continue;
    }

    private static bool HasWon(Board board, Position position, Symbol symbol) =>
        CheckAxis(board, position, symbol, 1, 0) ||
        CheckAxis(board, position, symbol, 0, 1) ||
        CheckAxis(board, position, symbol, 1, 1) ||
        CheckAxis(board, position, symbol, 1, -1);

    private static bool CheckAxis(Board board, Position position, Symbol symbol, int deltaX, int deltaY)
    {
        var total = 1
            + CountInDirection(board, position, symbol, deltaX, deltaY)
            + CountInDirection(board, position, symbol, -deltaX, -deltaY);

        return total >= 5;
    }

    private static int CountInDirection(
        Board board,
        Position position,
        Symbol symbol,
        int deltaX,
        int deltaY)
    {
        var count = 0;
        var x = position.X + deltaX;
        var y = position.Y + deltaY;

        while (x >= 0 && x < board.Size && y >= 0 && y < board.Size)
        {
            if (board.GetSymbol(new Position(x, y)) != symbol)
                break;

            count++;
            x += deltaX;
            y += deltaY;
        }

        return count;
    }
}

