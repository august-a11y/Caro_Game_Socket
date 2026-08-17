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
            // Set room status to Playing when the first move is applied
            if (Status == RoomStatus.Waiting)
                Status = RoomStatus.Playing;

            CurrentTurn = userId == PlayerA.PlayerId
                ? PlayerB.PlayerId
                : PlayerA.PlayerId;

            TurnDeadline = DateTime.UtcNow.AddSeconds(TurnDurationSec);
        }

        public MatchResultType CheckWinCondition()
        {
            if (_moveHistory.Count == 0)
                return MatchResultType.Continue;

            var last = _moveHistory[^1];
            var pos = last.Position;
            var symbol = last.Symbol;

            // Directions: (dx,dy)
            (int dx, int dy)[] directions = new[]
            {
                (1, 0), // horizontal
                (0, 1), // vertical
                (1, 1), // diag down-right
                (1, -1) // diag up-right
            };

            foreach (var (dx, dy) in directions)
            {
                var count = 1; // include last move

                // scan negative direction
                for (int step = 1; ; step++)
                {
                    var nx = pos.X - step * dx;
                    var ny = pos.Y - step * dy;
                    if (nx < 0 || nx >= Board.Size || ny < 0 || ny >= Board.Size)
                        break;
                    var s = Board.GetSymbol(new Position(nx, ny));
                    if (s == symbol)
                        count++;
                    else
                        break;
                }

                // scan positive direction
                for (int step = 1; ; step++)
                {
                    var nx = pos.X + step * dx;
                    var ny = pos.Y + step * dy;
                    if (nx < 0 || nx >= Board.Size || ny < 0 || ny >= Board.Size)
                        break;
                    var s = Board.GetSymbol(new Position(nx, ny));
                    if (s == symbol)
                        count++;
                    else
                        break;
                }

                if (count >= 5)
                {
                    return symbol == PlayerA.Symbol
                        ? MatchResultType.PlayerAWin
                        : MatchResultType.PlayerBWin;
                }
            }

            if (Board.IsFull())
                return MatchResultType.Draw;

            return MatchResultType.Continue;
        }

        public void EndMatch(MatchResultType result)
        {
            Status = RoomStatus.Finished;
            // Additional end-match bookkeeping could be added here
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
