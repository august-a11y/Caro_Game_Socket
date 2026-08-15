using CaroGame.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaroGame.Application.Contracts
{
    public sealed record MoveInfo
    (
        int MoveNumber,
        string PlayerId,
        int x,
        int y,
        Symbol Symbol,
        DateTime Timestamp
    );
}
