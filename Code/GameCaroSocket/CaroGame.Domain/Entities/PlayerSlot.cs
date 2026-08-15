using CaroGame.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaroGame.Domain.Entities
{
    public sealed record PlayerSlot(
    Guid PlayerId,
    Symbol Symbol
    );
}
