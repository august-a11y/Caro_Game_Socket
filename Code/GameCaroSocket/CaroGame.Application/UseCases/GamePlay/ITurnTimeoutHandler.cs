using System.Threading;

namespace CaroGame.Application.UseCases.GamePlay
{
    public interface ITurnTimeoutHandler
    {
        Task HandleTurnTimeoutAsync(Guid roomId, Guid playerId, CancellationToken cancellationToken);
    }
}