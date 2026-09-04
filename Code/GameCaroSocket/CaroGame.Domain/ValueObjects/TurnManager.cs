namespace CaroGame.Domain.ValueObjects
{
    public sealed class TurnManager
    {
        private TimeSpan _pausedRemaining;

        public Guid CurrentTurnPlayerId { get; private set; }
        public DateTime TurnStartedAt { get; private set; }
        public DateTime TurnDeadline { get; private set; }
        public int DurationInSeconds { get; }
        public bool IsPaused { get; private set; }

        public TurnManager(Guid startingPlayerId, int durationInSeconds, DateTime startTime)
        {
            if (startingPlayerId == Guid.Empty)
                throw new ArgumentException("Starting player must not be empty.", nameof(startingPlayerId));
            if (durationInSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(durationInSeconds), "Turn duration must be greater than zero.");

            CurrentTurnPlayerId = startingPlayerId;
            DurationInSeconds = durationInSeconds;
            TurnStartedAt = startTime;
            TurnDeadline = startTime.AddSeconds(durationInSeconds);
        }

        public void SwitchTurn(Guid nextPlayerId, DateTime currentTime)
        {
            if (nextPlayerId == Guid.Empty)
                throw new ArgumentException("Next player must not be empty.", nameof(nextPlayerId));

            CurrentTurnPlayerId = nextPlayerId;
            TurnStartedAt = currentTime;
            TurnDeadline = currentTime.AddSeconds(DurationInSeconds);
            _pausedRemaining = TimeSpan.Zero;
            IsPaused = false;
        }

        public void Pause(DateTime currentTime)
        {
            if (IsPaused)
                return;
            if (currentTime < TurnStartedAt)
                throw new ArgumentOutOfRangeException(nameof(currentTime), "Pause time cannot precede the turn.");

            _pausedRemaining = TurnDeadline > currentTime
                ? TurnDeadline - currentTime
                : TimeSpan.Zero;
            IsPaused = true;
        }

        public void Resume(DateTime currentTime)
        {
            if (!IsPaused)
                return;

            TurnStartedAt = currentTime;
            TurnDeadline = currentTime.Add(_pausedRemaining);
            _pausedRemaining = TimeSpan.Zero;
            IsPaused = false;
        }

        public bool IsTimeUp(DateTime currentTime)
        {
            return !IsPaused && currentTime >= TurnDeadline;
        }
    }
}
