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
        public DateTime? ClosedAt { get; private set; }

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

        public void Accept(DateTime? timestamp = null)
        {
            EnsurePending();

            Status = ChallengeStatus.Accepted;
            ClosedAt = timestamp ?? DateTime.UtcNow;
        }

        public void Reject(DateTime? timestamp = null)
        {
            EnsurePending();

            Status = ChallengeStatus.Rejected;
            ClosedAt = timestamp ?? DateTime.UtcNow;
        }

        public void Cancel(DateTime? timestamp = null)
        {
            EnsurePending();
            Status = ChallengeStatus.Cancelled;
            ClosedAt = timestamp ?? DateTime.UtcNow;
        }

        public void Expire(DateTime? timestamp = null)
        {
            if (Status == ChallengeStatus.Pending)
            {
                Status = ChallengeStatus.Expired;
                ClosedAt = timestamp ?? DateTime.UtcNow;
            }
        }

        public bool IsExpired(DateTime timestamp) => timestamp >= ExpiresAt;

        private void EnsurePending()
        {
            if (Status != ChallengeStatus.Pending)
                throw new InvalidOperationException("Challenge is no longer pending.");
        }
    }
}
