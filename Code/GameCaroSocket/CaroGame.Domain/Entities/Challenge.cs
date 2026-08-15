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
            TimeSpan expiration)
        {
            ChallengeId = Guid.NewGuid();

            FromPlayerId = fromPlayerId;
            ToPlayerId = toPlayerId;

            Status = ChallengeStatus.Pending;

            CreatedAt = DateTime.UtcNow;

            ExpiresAt = CreatedAt.Add(expiration);
        }

        public void Accept()
        {
            if (Status != ChallengeStatus.Pending)
                throw new InvalidOperationException(
                    "Challenge is no longer pending.");

            Status = ChallengeStatus.Accepted;
        }

        public void Reject()
        {
            if (Status != ChallengeStatus.Pending)
                throw new InvalidOperationException(
                    "Challenge is no longer pending.");

            Status = ChallengeStatus.Rejected;
        }

        public void Expire()
        {
            if (Status == ChallengeStatus.Pending)
                Status = ChallengeStatus.Expired;
        }
    }
}
