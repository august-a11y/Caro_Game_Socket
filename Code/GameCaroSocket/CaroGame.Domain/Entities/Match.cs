using CaroGame.Domain.Enum;
using CaroGame.Domain.ValueObjects;

namespace CaroGame.Domain.Entities
{
    public class Match
    {
        public Board Board { get; }
        public TurnManager TurnManager { get; }
        private readonly List<Move> _moveHistory = new();
        public Guid PlayerXId { get; }
        public Guid PlayerOId { get; }
        public MatchResultType matchResult { get; set; }

        public IReadOnlyCollection<Move> MoveHistory => _moveHistory;

        public Match(Guid playerXId, Guid playerOId)
        {
            PlayerXId = playerXId;
            PlayerOId = playerOId;
            Board = new Board();
            TurnManager = new TurnManager(playerXId, 30, DateTime.UtcNow);
        }

        public void ApplyMove(Guid playerId, Position position)
        {
            if (TurnManager.CurrentTurnPlayerId != playerId)
                throw new InvalidOperationException("It's not the player's turn.");
            if(playerId != PlayerXId && playerId != PlayerOId   )
                throw new InvalidOperationException("User is not a player in this room.");
            Board.PlaceSymbol(position, GetPlayerSymbol(playerId));
            _moveHistory.Add(new Move(_moveHistory.Count + 1,
                    playerId,
                    position,
                    GetPlayerSymbol(playerId),
                    DateTime.UtcNow));
            var nextPlayerId = playerId == PlayerXId ? PlayerOId : PlayerXId;
            TurnManager.SwitchTurn(nextPlayerId, DateTime.UtcNow);
        }

        public void EndMatch(MatchResultType result)
        {
            matchResult = result;
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
                if (count >= 5) return true;
            }
            return false;
        }

        private Symbol GetPlayerSymbol(Guid playerId)
        {
            if (playerId == PlayerXId)
                return Symbol.X;

            if (playerId == PlayerOId)
                return Symbol.O;

            throw new InvalidOperationException(
                "User is not a player in this room.");
        }
        
    }

   
}
