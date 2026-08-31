using CaroGame.Domain.Entities;
using System.Threading;

namespace CaroGame.Application.UseCases.SessionUseCase
{
    public interface IPlayerSessionJoiner
    {
        Task<Session> JoinAsync(string nickname, CancellationToken cancellationToken);
    }
}
