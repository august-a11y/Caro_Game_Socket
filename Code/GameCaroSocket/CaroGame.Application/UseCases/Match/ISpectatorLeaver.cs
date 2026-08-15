using CaroGame.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaroGame.Application.UseCases.Spectator
{
    public interface ILeaveSpectatorUseCase
    {
        Task<Room> LeaveSpectator(Guid roomId, Guid playerId);
    }
}
