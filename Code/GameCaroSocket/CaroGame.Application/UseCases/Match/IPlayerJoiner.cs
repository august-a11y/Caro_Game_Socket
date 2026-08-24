using CaroGame.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaroGame.Application.UseCases.Match
{
    public interface IPlayerJoiner
    {
        Task<Room> JoinPlayerAsync(Guid playerId, Guid roomId, CancellationToken cancellationToken);
    }
}
