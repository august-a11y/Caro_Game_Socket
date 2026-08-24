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
            PlayerId = playerId;
            DisconnectedAt = disconnectedAt;
            GracePeriodEndsAt = gracePeriodEndsAt;
        }
    }
}
