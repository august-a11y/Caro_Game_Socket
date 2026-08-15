using System;
using System.Collections.Generic;
using System.Text;

namespace CaroGame.Domain.ValueObjects
{
    public readonly record struct Position(int X, int Y);
}
