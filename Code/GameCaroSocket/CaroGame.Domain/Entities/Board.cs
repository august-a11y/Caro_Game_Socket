using CaroGame.Domain.Enum;
using CaroGame.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaroGame.Domain.Entities
{
    public sealed class Board
    {
        public readonly Symbol?[,] _cells;
        public int _placedCount = 0;
        public readonly int _maxCapacity;

        public int Size { get; }

        public Board(int size = 15)
        {
            if (size <= 0)
                throw new ArgumentOutOfRangeException(nameof(size));

            Size = size;
            _cells = new Symbol?[size, size];
            _maxCapacity = size * size;
        }

        public void PlaceSymbol(Position position, Symbol symbol)
        {
            if (!IsInBounds(position)) return ;

            if (_cells[position.Y, position.X] != null) return ;

            _cells[position.Y, position.X] = symbol;
            _placedCount++;

        }

        private bool IsInBounds(Position position)
        {
            return position.X >= 0 &&
                   position.X < Size &&
                   position.Y >= 0 &&
                   position.Y < Size;
        }
        public bool IsFull() => _placedCount >= _maxCapacity;
    }

}
