using CaroGame.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaroGame.Application.UseCases.Match
{
    public interface ISpectatorJoiner
    {
        Room JoinSpectator(
            Guid roomId,
            Guid playerId);
    }
}

