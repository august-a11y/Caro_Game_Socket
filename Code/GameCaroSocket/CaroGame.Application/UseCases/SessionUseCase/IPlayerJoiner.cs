using CaroGame.Domain.Entities;

namespace CaroGame.Application.UseCases.SessionUseCase
{
    public interface IPlayerJoiner
    {
        Task<Session> JoinAsync(string nickname, CancellationToken cancellationToken);
    }
}

