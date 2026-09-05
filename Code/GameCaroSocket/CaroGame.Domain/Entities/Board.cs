using CaroGame.Domain.Enum;
using CaroGame.Domain.ValueObjects;
namespace CaroGame.Domain.Entities;

public sealed class Board
{
    private readonly Symbol?[,] _cells;

    public int Size { get; }
    public int PlacedCount { get; private set; }

    public Board(int size = 15)
    {
        if (size <= 0)
            throw new ArgumentOutOfRangeException(nameof(size), "Board size must be greater than zero.");

        Size = size;
        _cells = new Symbol?[size, size];
    }

    public Symbol? GetSymbol(Position position)
    {
        EnsureInBounds(position);
        return _cells[position.Y, position.X];
    }

    public void PlaceSymbol(Position position, Symbol symbol)
    {
        EnsureInBounds(position);

        if (!System.Enum.IsDefined(symbol))
            throw new ArgumentOutOfRangeException(nameof(symbol), symbol, "Symbol must be X or O.");

        if (_cells[position.Y, position.X] is not null)
            throw new InvalidOperationException("The selected position is already occupied.");

        _cells[position.Y, position.X] = symbol;
        PlacedCount++;
    }

    public bool IsFull() => PlacedCount == Size * Size;

    private void EnsureInBounds(Position position)
    {
        if (position.X < 0 || position.X >= Size || position.Y < 0 || position.Y >= Size)
            throw new ArgumentOutOfRangeException(nameof(position), "Position must be inside the board.");
    }
}
