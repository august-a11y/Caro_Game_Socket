using CaroGame.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaroGame.Application.UseCases.Match
{
    public interface IPlayerLeaver
    {
        Task<Room> LeavePlayerAsync(Guid playerId, Guid roomId, CancellationToken cancellationToken);
    }
}
