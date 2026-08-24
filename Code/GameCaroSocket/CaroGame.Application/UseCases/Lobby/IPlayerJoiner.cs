using CaroGame.Application.Contracts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CaroGame.Application.UseCases.Lobby
{
    public interface IPlayerJoiner
    {
        Task<PlayerInfo> JoinAsync(Guid playerId, CancellationToken cancellationToken);
    }
}
