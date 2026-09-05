using CaroGame.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaroGame.Domain.Entities
{
    public sealed class Challenge
    {
        public Guid ChallengeId { get; }

        public Guid FromPlayerId { get; }

        public Guid ToPlayerId { get; }

        public ChallengeStatus Status { get; private set; }

        public DateTime CreatedAt { get; }

        public DateTime ExpiresAt { get; }

        public Challenge(
            Guid fromPlayerId,
            Guid toPlayerId,
            TimeSpan expiration,
            DateTime? createdAt = null)
        {
            if (fromPlayerId == Guid.Empty)
                throw new ArgumentException("Challenger must have a valid identifier.", nameof(fromPlayerId));
            if (toPlayerId == Guid.Empty)
                throw new ArgumentException("Opponent must have a valid identifier.", nameof(toPlayerId));
            if (fromPlayerId == toPlayerId)
                throw new ArgumentException("A player cannot challenge themselves.", nameof(toPlayerId));
            if (expiration <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(expiration), "Expiration must be greater than zero.");

            ChallengeId = Guid.NewGuid();

            FromPlayerId = fromPlayerId;
            ToPlayerId = toPlayerId;

            Status = ChallengeStatus.Pending;

            CreatedAt = createdAt ?? DateTime.UtcNow;

            ExpiresAt = CreatedAt.Add(expiration);
        }

        public void Accept()
        {
            EnsurePending();

            Status = ChallengeStatus.Accepted;
        }

        public void Reject()
        {
            EnsurePending();

            Status = ChallengeStatus.Rejected;
        }

        public void Expire()
        {
            if (Status == ChallengeStatus.Pending)
                Status = ChallengeStatus.Expired;
        }

        public bool IsExpired(DateTime timestamp) => timestamp >= ExpiresAt;

        private void EnsurePending()
        {
            if (Status != ChallengeStatus.Pending)
                throw new InvalidOperationException("Challenge is no longer pending.");
        }
    }
}
