using CaroGame.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaroGame.Application.Contracts
{
    public sealed record PlayerInfo
    {
        public Guid UserId { get; init; }

        public string Nickname { get; init; } = string.Empty;

        public PlayerStatus Status { get; init; }
    }
}
