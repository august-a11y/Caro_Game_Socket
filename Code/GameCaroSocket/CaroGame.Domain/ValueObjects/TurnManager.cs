namespace CaroGame.Domain.ValueObjects
{
    public class TurnManager
    {
        public Guid CurrentTurnPlayerId { get; set; }
        public DateTime TurnDeadline { get; set; }
        public int DurationInSeconds { get; set; }

        public TurnManager(Guid startingPlayerId, int durationInSeconds, DateTime startTime)
        {
            CurrentTurnPlayerId = startingPlayerId;
            DurationInSeconds = durationInSeconds;
            TurnDeadline = startTime.AddSeconds(durationInSeconds);
        }

        public void SwitchTurn(Guid nextPlayerId, DateTime currentTime)
        {
            CurrentTurnPlayerId = nextPlayerId;
            TurnDeadline = currentTime.AddSeconds(DurationInSeconds);
        }

        public bool IsTimeUp(DateTime currentTime)
        {
            return currentTime >= TurnDeadline;
        }
    }
}