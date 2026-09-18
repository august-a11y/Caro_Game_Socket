namespace CaroGame.Application.UseCases.GamePlay
{
    public interface ITurnTimeoutHandler
    {
        void HandleTurnTimeout(Guid roomId, Guid playerId);
    }
}

