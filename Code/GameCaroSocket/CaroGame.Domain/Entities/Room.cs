using CaroGame.Domain.Enum;
using CaroGame.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaroGame.Domain.Entities
{
    public sealed class Room
    {
        private readonly List<Move> _moveHistory = new();
        private readonly HashSet<Guid> _spectators = new();
        private readonly Dictionary<Guid, DisconnectInfo> _disconnected = new();

        public Guid RoomId { get; }

        public PlayerSlot PlayerA { get; }
        public PlayerSlot PlayerB { get; }

        public Board Board { get; }

        public Guid CurrentTurn { get; private set; }

        public RoomStatus Status { get; private set; }

        public IReadOnlyCollection<Move> MoveHistory => _moveHistory;

        public IReadOnlyCollection<Guid> Spectators => _spectators;

        public IReadOnlyDictionary<Guid, DisconnectInfo> Disconnected =>
            _disconnected;

        public int TurnDurationSec { get; }

        public DateTime TurnDeadline { get; private set; }

        public DateTime CreatedAt { get; }

        public Room(
            PlayerSlot playerA,
            PlayerSlot playerB,
            int boardSize = 15,
            int turnDurationSec = 30)
        {
            RoomId = Guid.NewGuid();

            PlayerA = playerA;
            PlayerB = playerB;

            Board = new Board(boardSize);

            CurrentTurn = playerA.PlayerId;

            Status = RoomStatus.Waiting;

            TurnDurationSec = turnDurationSec;

            CreatedAt = DateTime.UtcNow;

            TurnDeadline = CreatedAt.AddSeconds(turnDurationSec);
        }

        public void ApplyMove(Guid userId, Position position)
        {
            if (GetRole(userId) != Role.Player)
                throw new InvalidOperationException(
                    "Only players can make moves.");

            if (CurrentTurn != userId)
                throw new InvalidOperationException(
                    "It is not your turn.");

            var symbol = GetPlayerSymbol(userId);

            Board.PlaceSymbol(position, symbol);

            _moveHistory.Add(
                new Move(
                    _moveHistory.Count + 1,
                    userId,
                    position,
                    symbol,
                    DateTime.UtcNow));

            CurrentTurn = userId == PlayerA.PlayerId
                ? PlayerB.PlayerId
                : PlayerA.PlayerId;

            TurnDeadline = DateTime.UtcNow.AddSeconds(TurnDurationSec);
        }

        public MatchResultType CheckWinCondition()
        {
            if (_moveHistory.Count == 0)
                return MatchResultType.Continue;

            var lastMove = _moveHistory[_moveHistory.Count - 1];
            if (Check5InARow(lastMove.Position, lastMove.Symbol))
                return MatchResultType.Win;

            if (Board.IsFull())
                return MatchResultType.Draw;

            return MatchResultType.Continue;
        }

        private bool Check5InARow(Position pos, Symbol symbol)
        {
            var directions = new (int dx, int dy)[]
            {
                (1, 0),  // Ngang
                (0, 1),  // Dọc
                (1, 1),  // Chéo chính
                (1, -1)  // Chéo phụ
            };

            foreach (var (dx, dy) in directions)
            {
                int count = 1;

                // Duyệt chiều dương
                for (int i = 1; i <= 4; i++)
                {
                    var checkPos = new Position(pos.X + dx * i, pos.Y + dy * i);
                    if (Board.GetSymbol(checkPos) == symbol) count++;
                    else break;
                }

                // Duyệt chiều âm
                for (int i = 1; i <= 4; i++)
                {
                    var checkPos = new Position(pos.X - dx * i, pos.Y - dy * i);
                    if (Board.GetSymbol(checkPos) == symbol) count++;
                    else break;
                }

                // Luật chơi: Nếu đúng 5 con liên tiếp thì thắng
                if (count >= 5) return true;
            }

            return false;
        }

        public Role GetRole(Guid userId)
        {
            if (userId == PlayerA.PlayerId ||
                userId == PlayerB.PlayerId)
            {
                return Role.Player;
            }

            if (_spectators.Contains(userId))
                return Role.Spectator;

            return Role.None;
        }

        public void AddSpectator(Guid userId)
        {
            if (GetRole(userId) == Role.None)
                _spectators.Add(userId);
        }

        public void RemoveSpectator(Guid userId)
        {
            _spectators.Remove(userId);
        }

        public void MarkDisconnected(
            Guid userId,
            int gracePeriodSeconds)
        {
            var disconnectedAt = DateTime.UtcNow;

            var gracePeriodEndsAt =
                disconnectedAt.AddSeconds(gracePeriodSeconds);

            _disconnected[userId] = new DisconnectInfo(
                userId,
                disconnectedAt,
                gracePeriodEndsAt);
        }

        public void MarkReconnected(Guid userId)
        {
            _disconnected.Remove(userId);
        }

        private Symbol GetPlayerSymbol(Guid userId)
        {
            if (userId == PlayerA.PlayerId)
                return PlayerA.Symbol;

            if (userId == PlayerB.PlayerId)
                return PlayerB.Symbol;

            throw new InvalidOperationException(
                "User is not a player in this room.");
        }
    }
}
