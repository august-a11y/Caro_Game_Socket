using CaroGame.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaroGame.Application.UseCases.Spectator
{
    public interface ISpectatorJoiner
    {
        Task<Room> JoinSpectator(Guid roomId, Guid playerId);
    }
}
