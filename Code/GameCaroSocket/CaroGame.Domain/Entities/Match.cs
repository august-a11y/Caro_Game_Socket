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
