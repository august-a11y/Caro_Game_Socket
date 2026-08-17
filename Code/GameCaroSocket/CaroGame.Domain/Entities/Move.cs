using CaroGame.Domain.Enum;
using CaroGame.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaroGame.Domain.Entities
{
    public sealed class Move
    {
        public int MoveNumber { get; }
        public Guid PlayerId { get; }
        public Position Position { get; }
        public Symbol Symbol { get; }
        public DateTime Timestamp { get; }

        public Move(
            int moveNumber,
            Guid playerId,
            Position position,
            Symbol symbol,
            DateTime timestamp)
        {
            MoveNumber = moveNumber;
            PlayerId = playerId;
            Position = position;
            Symbol = symbol;
            Timestamp = timestamp;
        }
    }
}
