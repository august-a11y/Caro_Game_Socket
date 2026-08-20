using CaroGame.Domain.Enum;
using CaroGame.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaroGame.Domain.Entities
{
    public sealed class Room
    {

        private readonly HashSet<Guid> _spectators = new();
        private readonly Dictionary<Guid, DisconnectInfo> _disconnected = new();

        public Guid RoomId { get; }
        public RoomStatus Status { get; private set; }
        public PlayerSlot PlayerX { get; private set; }
        public PlayerSlot PlayerO { get; private set; }
        public Match CurrentMatch { get; private set; }

        public IReadOnlyCollection<Guid> Spectators => _spectators;

        public IReadOnlyDictionary<Guid, DisconnectInfo> Disconnected =>
            _disconnected;


        public DateTime CreatedAt { get; }

        public Room(
            PlayerSlot playerX,
            PlayerSlot playerO,
            int boardSize = 15,
            int turnDurationSec = 30)
        {
            RoomId = Guid.NewGuid();
            PlayerX = playerX;
            PlayerO = playerO;

            Status = RoomStatus.Waiting;


            CreatedAt = DateTime.UtcNow;

        }

        public void StartNewMatch(DateTime startTime)
        {
            if(Status != RoomStatus.Playing)
                throw new InvalidOperationException(
                    "Room is already playing.");
            CurrentMatch = new Match(PlayerX.PlayerId, PlayerO.PlayerId);
        }
        
    }
}
