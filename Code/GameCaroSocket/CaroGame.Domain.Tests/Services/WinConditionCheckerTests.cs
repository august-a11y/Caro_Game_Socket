using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;
using CaroGame.Domain.Services;
using CaroGame.Domain.ValueObjects;

namespace CaroGame.Domain.Tests.Services;

public sealed class WinConditionCheckerTests
{
    private readonly WinConditionChecker _checker = new();

    [Fact]
    public void Check_WhenBoardIsNull_Throws()
    {
        var move = CreateMove(1, new Position(0, 0), Symbol.X);

        Assert.Throws<ArgumentNullException>(() => _checker.Check(null!, move));
    }

    [Fact]
    public void Check_WhenLastMoveIsNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _checker.Check(new Board(), null!));
    }

    [Fact]
    public void Check_WhenXHasFiveHorizontalSymbols_ReturnsPlayerXWin()
    {
        var positions = new[]
        {
            new Position(2, 7),
            new Position(3, 7),
            new Position(4, 7),
            new Position(5, 7),
            new Position(6, 7)
        };

        AssertWin(positions, positions[2], Symbol.X, MatchResultType.PlayerXWin);
    }

    [Fact]
    public void Check_WhenOHasFiveVerticalSymbols_ReturnsPlayerOWin()
    {
        var positions = new[]
        {
            new Position(9, 3),
            new Position(9, 4),
            new Position(9, 5),
            new Position(9, 6),
            new Position(9, 7)
        };

        AssertWin(positions, positions[2], Symbol.O, MatchResultType.PlayerOWin);
    }

    [Fact]
    public void Check_WhenXHasFiveDownwardDiagonalSymbols_ReturnsPlayerXWin()
    {
        var positions = new[]
        {
            new Position(1, 2),
            new Position(2, 3),
            new Position(3, 4),
            new Position(4, 5),
            new Position(5, 6)
        };

        AssertWin(positions, positions[2], Symbol.X, MatchResultType.PlayerXWin);
    }

    [Fact]
    public void Check_WhenOHasFiveUpwardDiagonalSymbols_ReturnsPlayerOWin()
    {
        var positions = new[]
        {
            new Position(4, 8),
            new Position(5, 7),
            new Position(6, 6),
            new Position(7, 5),
            new Position(8, 4)
        };

        AssertWin(positions, positions[2], Symbol.O, MatchResultType.PlayerOWin);
    }

    [Fact]
    public void Check_WhenThereAreOnlyFourConsecutiveSymbols_ReturnsContinue()
    {
        var board = new Board();
        var positions = new[]
        {
            new Position(5, 10),
            new Position(6, 10),
            new Position(7, 10),
            new Position(8, 10)
        };

        foreach (var position in positions)
            board.PlaceSymbol(position, Symbol.X);

        var lastMove = CreateMove(positions.Length, positions[^1], Symbol.X);

        Assert.Equal(MatchResultType.Continue, _checker.Check(board, lastMove));
    }

    [Fact]
    public void Check_WhenLineContainsGap_ReturnsContinue()
    {
        var board = new Board();
        var positions = new[]
        {
            new Position(2, 11),
            new Position(3, 11),
            new Position(5, 11),
            new Position(6, 11),
            new Position(7, 11)
        };

        foreach (var position in positions)
            board.PlaceSymbol(position, Symbol.X);

        Assert.Equal(
            MatchResultType.Continue,
            _checker.Check(board, CreateMove(5, positions[^1], Symbol.X)));
    }

    [Fact]
    public void Check_WhenThereAreMoreThanFiveSymbols_ReturnsWin()
    {
        var positions = Enumerable.Range(1, 6)
            .Select(x => new Position(x, 12))
            .ToArray();

        AssertWin(positions, positions[3], Symbol.X, MatchResultType.PlayerXWin);
    }

    [Fact]
    public void Check_WhenBoardIsFullWithoutFiveInARow_ReturnsDraw()
    {
        var board = new Board(3);
        var symbols = new[,]
        {
            { Symbol.X, Symbol.O, Symbol.X },
            { Symbol.O, Symbol.X, Symbol.O },
            { Symbol.O, Symbol.X, Symbol.O }
        };

        for (var y = 0; y < board.Size; y++)
        for (var x = 0; x < board.Size; x++)
            board.PlaceSymbol(new Position(x, y), symbols[y, x]);

        var lastPosition = new Position(2, 2);
        var lastMove = CreateMove(9, lastPosition, Symbol.O);

        Assert.Equal(MatchResultType.Draw, _checker.Check(board, lastMove));
    }

    [Fact]
    public void Check_WhenLastMoveDoesNotMatchBoard_Throws()
    {
        var board = new Board();
        var position = new Position(4, 4);
        board.PlaceSymbol(position, Symbol.X);
        var inconsistentMove = CreateMove(1, position, Symbol.O);

        Assert.Throws<ArgumentException>(() => _checker.Check(board, inconsistentMove));
    }

    private void AssertWin(
        IEnumerable<Position> positions,
        Position lastPosition,
        Symbol symbol,
        MatchResultType expectedResult)
    {
        var board = new Board();
        var moveNumber = 0;

        foreach (var position in positions)
        {
            board.PlaceSymbol(position, symbol);
            moveNumber++;
        }

        var lastMove = CreateMove(moveNumber, lastPosition, symbol);
        Assert.Equal(expectedResult, _checker.Check(board, lastMove));
    }

    private static Move CreateMove(int moveNumber, Position position, Symbol symbol) =>
        new(moveNumber, Guid.NewGuid(), position, symbol, DateTime.UnixEpoch);
}
