using System;
using System.Collections.Generic;
using System.Text;

namespace CaroGame.Domain.ValueObjects
{
    public sealed class DisconnectInfo
    {
        public Guid PlayerId { get; }
        public DateTime DisconnectedAt { get; }
        public DateTime GracePeriodEndsAt { get; }

        public DisconnectInfo(
            Guid playerId,
            DateTime disconnectedAt,
            DateTime gracePeriodEndsAt)
        {
            if (playerId == Guid.Empty)
                throw new ArgumentException("Player identifier must not be empty.", nameof(playerId));
            if (gracePeriodEndsAt <= disconnectedAt)
                throw new ArgumentException(
                    "Grace period must end after the disconnection time.",
                    nameof(gracePeriodEndsAt));

            PlayerId = playerId;
            DisconnectedAt = disconnectedAt;
            GracePeriodEndsAt = gracePeriodEndsAt;
        }
    }
}
