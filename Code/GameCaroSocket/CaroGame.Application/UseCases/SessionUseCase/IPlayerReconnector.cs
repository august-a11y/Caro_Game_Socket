using CaroGame.Domain.Entities;

namespace CaroGame.Application.UseCases.SessionUseCase
{
    public interface IPlayerReconnector
    {
        Task<Session> ReconnectPlayerAsync(Guid playerId, Guid sessionId, CancellationToken cancellationToken);
    }
}

