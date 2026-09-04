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

        public DateTime? DisconnectedAt { get; private set; }

        public bool IsConnected { get; private set; } = true;

        public Session(Guid playerId, Guid sessionId, DateTime? createdAt = null)
        {
            if (playerId == Guid.Empty)
                throw new ArgumentException("Player identifier must not be empty.", nameof(playerId));
            if (sessionId == Guid.Empty)
                throw new ArgumentException("Session identifier must not be empty.", nameof(sessionId));

            PlayerId = playerId;
            SessionId = sessionId;
            LastHeartbeatAt = createdAt ?? DateTime.UtcNow;
        }

        public void UpdateHeartbeat(DateTime? timestamp = null)
        {
            if (!IsConnected)
                throw new InvalidOperationException("A disconnected session cannot send heartbeat messages.");

            LastHeartbeatAt = timestamp ?? DateTime.UtcNow;
        }

        public void MarkDisconnected(DateTime? timestamp = null)
        {
            if (!IsConnected)
                return;

            IsConnected = false;
            DisconnectedAt = timestamp ?? DateTime.UtcNow;
        }

        public void MarkReconnected(DateTime? timestamp = null)
        {
            IsConnected = true;
            DisconnectedAt = null;
            LastHeartbeatAt = timestamp ?? DateTime.UtcNow;
        }

        public void SetUdpEndpoint(string address, int port)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Address cannot be empty.", nameof(address));
            if (port is < 1 or > 65535)
                throw new ArgumentOutOfRangeException(nameof(port), "Port must be between 1 and 65535.");

            UdpEndpoint = $"{address.Trim()}:{port}";
        }
    }
}
