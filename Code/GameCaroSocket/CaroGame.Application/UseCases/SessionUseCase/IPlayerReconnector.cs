using CaroGame.Domain.Entities;

namespace CaroGame.Application.UseCases.SessionUseCase
{
    public interface IPlayerReconnector
    {
        Session ReconnectPlayer(Guid playerId, Guid sessionId);
    }
}

