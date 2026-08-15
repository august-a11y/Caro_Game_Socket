using CaroGame.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaroGame.Application.Contracts
{
    public sealed record RoomSummary
    {
        public Guid RoomId { get; init; }

        public string PlayerAName { get; init; } = string.Empty;

        public string PlayerBName { get; init; } = string.Empty;

        public int SpectatorCount { get; init; }

        public RoomStatus Status { get; init; }

        public DateTime StartedAt { get; init; }
    }
}
