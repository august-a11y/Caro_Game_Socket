using CaroGame.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaroGame.Application.Contracts
{
    public sealed record RoomState
    {
        public Guid RoomId { get; init; }

        public List<List<Symbol?>> Board { get; init; } = null!;

        public PlayerSlotState PlayerA { get; init; } = null!;

        public PlayerSlotState PlayerB { get; init; } = null!;

        public Guid CurrentTurn { get; init; }

        public RoomStatus Status { get; init; }

        public int TimeRemainingSec { get; init; }

        public int SpectatorCount { get; init; }

        public IReadOnlyList<MoveState> MoveHistory { get; init; }
            = [];
    }

    public sealed record PlayerSlotState
    {
        public Guid UserId { get; init; }
        

        public Symbol Symbol { get; init; }
    }

    public sealed record MoveState
    {
        public int X { get; init; }

        public int Y { get; init; }

        public Guid PlayerId { get; init; }
    }
}
