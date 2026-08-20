using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;
using CaroGame.Domain.ValueObjects;

namespace CaroGame.Domain.Services
{
    public class WinConditionChecker : IWinConditionChecker
    {

        public MatchResultType Check(Board board, Move lastMove)
        {
            var symbol = lastMove.Symbol;
            var pos = lastMove.Position;

            if (HasWon(board, pos, symbol))
            {
                return symbol == Symbol.X ? MatchResultType.PlayerXWin : MatchResultType.PlayerOWin;
            }

            if (board.IsFull()) 
            {
                return MatchResultType.Draw;
            }

            return MatchResultType.Continue;

        }




        private bool HasWon(Board board, Position pos, Symbol symbol)
        {
            return CheckAxis(board, pos, symbol, 1, 0) ||  
                   CheckAxis(board, pos, symbol, 0, 1) ||  
                   CheckAxis(board, pos, symbol, 1, 1) ||  
                   CheckAxis(board, pos, symbol, 1, -1);   
        }

        private bool CheckAxis(Board board, Position pos, Symbol symbol, int colDelta, int rowDelta)
        {
            int totalCount = 1 + CountInDirection(board._cells, pos, symbol, colDelta, rowDelta) + CountInDirection(board._cells, pos, symbol, -colDelta, -rowDelta); 

            return totalCount >= 5;

        }

        private int CountInDirection(Symbol?[,] board, Position position, Symbol symbol, int colDelta, int rowDelta)
        {
            int count = 0;

            int row = position.X + colDelta;
            int col = position.Y + rowDelta;

            while (IsInsideBoard(board, col, row) &&
                   board[row, col] == symbol)
            {
                count++;
                row += rowDelta;
                col += colDelta;
            }

            return count;
        }

        private bool IsInsideBoard(Symbol?[,] board, int col, int row)
        {
            int rowCount = board.GetLength(0);
            int colCount = board.GetLength(1);
            return row >= 0 && row < rowCount && col >= 0 && col < colCount;
        }
    }
}

