using CaroGame.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CaroGame.Application.UseCases.Match
{
    public interface ISpectatorJoiner
    {
        Task<Room> JoinSpectator(
            Guid roomId,
            Guid playerId,
            CancellationToken cancellationToken = default);
    }
}

