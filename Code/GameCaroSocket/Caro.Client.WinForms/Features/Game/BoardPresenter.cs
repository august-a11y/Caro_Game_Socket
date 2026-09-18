using CaroGame.Shared.Protocol.Contracts;

namespace Caro.Client.WinForms.Features.Game;

internal sealed class BoardPresenter
{
    public const int BoardSize = 15;
    private readonly string?[,] _board = new string?[BoardSize, BoardSize];

    public string?[,] Snapshot => (string?[,])_board.Clone();

    public void ApplySnapshot(RoomSnapshot room)
    {
        ArgumentNullException.ThrowIfNull(room);
        if (room.Board.Length != BoardSize || room.Board.Any(row => row.Length != BoardSize))
            throw new InvalidDataException("Kích thước bàn cờ từ máy chủ không hợp lệ.");
        for (var row = 0; row < BoardSize; row++)
            for (var column = 0; column < BoardSize; column++)
                SetCell(row, column, room.Board[row][column]);
    }

    public void ApplyMove(MoveDto move)
    {
        ArgumentNullException.ThrowIfNull(move);
        SetCell(move.Row, move.Column, move.Symbol);
    }

    private void SetCell(int row, int column, string? symbol)
    {
        if (row is < 0 or >= BoardSize || column is < 0 or >= BoardSize)
            throw new ArgumentOutOfRangeException(nameof(row));
        if (symbol is not (null or "" or "X" or "O"))
            throw new ArgumentException("Ô cờ chỉ được chứa X, O hoặc để trống.", nameof(symbol));
        _board[row, column] = string.IsNullOrEmpty(symbol) ? null : symbol;
    }
}
