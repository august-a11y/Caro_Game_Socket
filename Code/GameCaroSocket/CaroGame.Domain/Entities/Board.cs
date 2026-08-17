using CaroGame.Domain.Enum;
using CaroGame.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaroGame.Domain.Entities
{
    public sealed class Board
    {
        private readonly Symbol?[,] _cells;

        public int Size { get; }

        public Board(int size = 15)
        {
            if (size <= 0)
                throw new ArgumentOutOfRangeException(nameof(size));

            Size = size;
            _cells = new Symbol?[size, size];
        }
        public void PlaceSymbol(Position position, Symbol symbol)
        {
            if (!IsInBounds(position))
                throw new ArgumentOutOfRangeException(nameof(position));
            if (_cells[position.Y, position.X] is not null)
                throw new InvalidOperationException("Cell is already occupied.");
            _cells[position.Y, position.X] = symbol;
        }   

        public Symbol? GetSymbol(Position position)
        {
            if (!IsInBounds(position))
                throw new ArgumentOutOfRangeException(nameof(position));

            return _cells[position.Y, position.X];
        }
        

        private bool IsInBounds(Position position)
        {
            return position.X >= 0 &&
                   position.X < Size &&
                   position.Y >= 0 &&
                   position.Y < Size;
        }

        public bool IsFull()
        {
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    if (_cells[y, x] is null)
                        return false;
                }
            }

            return true;
        }
    }
}
