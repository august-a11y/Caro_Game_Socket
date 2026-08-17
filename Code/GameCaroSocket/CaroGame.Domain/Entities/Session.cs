using System;
using System.Collections.Generic;
using System.Text;

namespace CaroGame.Domain.Entities
{
    public sealed class Session
    {
        public Guid PlayerId { get; }

        public Guid SessionId { get; }

        public string? UdpEndpoint { get; private set; }

        public DateTime LastHeartbeatAt { get; private set; }

        public Session(Guid playerId, Guid sessionId)
        {
            PlayerId = playerId;
            SessionId = sessionId;
            LastHeartbeatAt = DateTime.UtcNow;
        }

        public void UpdateHeartbeat()
        {
            LastHeartbeatAt = DateTime.UtcNow;
        }

        public void SetUdpEndpoint(string address, int port)
        {
            UdpEndpoint = $"{address}:{port}";
        }
    }
}
