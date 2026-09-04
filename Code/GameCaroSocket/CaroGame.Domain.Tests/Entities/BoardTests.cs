using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;
using CaroGame.Domain.ValueObjects;

namespace CaroGame.Domain.Tests.Entities;

public sealed class BoardTests
{
    [Fact]
    public void Constructor_WithDefaultSize_CreatesEmptyFifteenByFifteenBoard()
    {
        var board = new Board();

        Assert.Equal(15, board.Size);
        Assert.Equal(0, board.PlacedCount);
        Assert.Null(board.GetSymbol(new Position(14, 14)));
        Assert.False(board.IsFull());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenSizeIsNotPositive_Throws(int size)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Board(size));
    }

    [Fact]
    public void PlaceSymbol_WhenPositionIsEmpty_StoresSymbolAndIncrementsCount()
    {
        var board = new Board(15);
        var position = new Position(4, 9);

        board.PlaceSymbol(position, Symbol.X);

        Assert.Equal(Symbol.X, board.GetSymbol(position));
        Assert.Null(board.GetSymbol(new Position(9, 4)));
        Assert.Equal(1, board.PlacedCount);
        Assert.False(board.IsFull());
    }

    [Fact]
    public void PlaceSymbol_WhenSymbolIsUndefined_ThrowsWithoutChangingBoard()
    {
        var board = new Board();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => board.PlaceSymbol(new Position(1, 2), (Symbol)999));
        Assert.Equal(0, board.PlacedCount);
    }

    [Fact]
    public void PlaceSymbol_WhenPositionIsOccupied_RejectsMoveWithoutChangingBoard()
    {
        var board = new Board();
        var position = new Position(2, 3);
        board.PlaceSymbol(position, Symbol.X);

        Assert.Throws<InvalidOperationException>(() => board.PlaceSymbol(position, Symbol.O));

        Assert.Equal(Symbol.X, board.GetSymbol(position));
        Assert.Equal(1, board.PlacedCount);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(15, 0)]
    [InlineData(0, 15)]
    public void PlaceSymbol_WhenPositionIsOutsideBoard_Throws(int x, int y)
    {
        var board = new Board();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => board.PlaceSymbol(new Position(x, y), Symbol.X));
        Assert.Equal(0, board.PlacedCount);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(15, 0)]
    [InlineData(0, 15)]
    public void GetSymbol_WhenPositionIsOutsideBoard_Throws(int x, int y)
    {
        var board = new Board();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => board.GetSymbol(new Position(x, y)));
    }

    [Fact]
    public void IsFull_WhenEveryPositionIsOccupied_ReturnsTrue()
    {
        var board = new Board(2);

        board.PlaceSymbol(new Position(0, 0), Symbol.X);
        board.PlaceSymbol(new Position(1, 0), Symbol.O);
        board.PlaceSymbol(new Position(0, 1), Symbol.O);

        Assert.False(board.IsFull());

        board.PlaceSymbol(new Position(1, 1), Symbol.X);

        Assert.True(board.IsFull());
        Assert.Equal(4, board.PlacedCount);
    }
}
