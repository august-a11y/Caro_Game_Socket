using CaroGame.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CaroGame.Application.UseCases.Match
{
    public interface ILeaveSpectatorUseCase
    {
        Task<Room> LeaveSpectator(Guid roomId, Guid playerId);
    }
}
