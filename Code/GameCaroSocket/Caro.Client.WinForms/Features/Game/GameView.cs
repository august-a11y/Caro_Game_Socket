using System;
using System.Drawing;
using System.Windows.Forms;
using Caro.Client.WinForms.Core;

namespace Caro.Client.WinForms;

public partial class GameView : UserControl
{
    private const int BoardSize = 15;

    private readonly Button[,] _cells =
        new Button[BoardSize, BoardSize];

    private bool _canMove;
    private readonly Button _surrender = new();
    private readonly Button _rematch = new();
    private readonly System.Windows.Forms.Timer _turnTimer = new() { Interval = 250 };
    private DateTime _turnDeadlineUtc;

    public event Action<int, int>? MoveRequested;
    public event Action? ReadyRequested;
    public event Action? RematchRequested;
    public event Action? SurrenderRequested;
    public event Action? LeaveRequested;

    public GameView()
    {
        InitializeComponent();
        DoubleBuffered = true;

        CreateBoard();

        pnlBoardHost.Resize += (_, _) => ResizeBoard();

        btnReady.Click += (_, _) => ReadyRequested?.Invoke();
        btnReady.FlatAppearance.MouseOverBackColor = Color.FromArgb(29, 78, 216);
        btnReady.FlatAppearance.MouseDownBackColor = Color.FromArgb(30, 64, 175);

        btnLeave.Click += (_, _) =>
            LeaveRequested?.Invoke();
        btnLeave.FlatAppearance.MouseOverBackColor = Color.FromArgb(254, 242, 242);
        btnLeave.FlatAppearance.MouseDownBackColor = Color.FromArgb(254, 226, 226);

        ConfigureActionButton(_surrender, "Đầu hàng", Color.White, Color.FromArgb(220, 38, 38));
        ConfigureActionButton(_rematch, "Chơi lại", Color.FromArgb(37, 99, 235), Color.White);
        _surrender.Click += (_, _) =>
        {
            var answer = MessageBox.Show(
                this,
                "Bạn có chắc muốn đầu hàng trận này?",
                "Xác nhận đầu hàng",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

            if (answer == DialogResult.Yes)
                SurrenderRequested?.Invoke();
        };
        _rematch.Click += (_, _) => RematchRequested?.Invoke();

        ResizeBoard();
        _turnTimer.Tick += (_, _) => UpdateCountdown();
        Disposed += (_, _) =>
        {
            _turnTimer.Dispose();
            if (!_surrender.IsDisposed) _surrender.Dispose();
            if (!_rematch.IsDisposed) _rematch.Dispose();
        };
    }

    private void CreateBoard()
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = BoardSize,
            ColumnCount = BoardSize,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = Color.FromArgb(190, 201, 217),
            CellBorderStyle = TableLayoutPanelCellBorderStyle.Single
        };

        // Bật DoubleBuffering cho TableLayoutPanel để fix giật lag khi thu phóng (resize)
        typeof(TableLayoutPanel)
            .GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(grid, true, null);

        grid.SuspendLayout();

        for (int i = 0; i < BoardSize; i++)
        {
            grid.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100F / BoardSize));

            grid.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100F / BoardSize));
        }

        for (int row = 0; row < BoardSize; row++)
        {
            for (int column = 0; column < BoardSize; column++)
            {
                var cell = new Button
                {
                    Dock = DockStyle.Fill,
                    Margin = Padding.Empty,
                    Padding = Padding.Empty,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(255, 253, 245),
                    ForeColor = Color.FromArgb(37, 99, 235),
                    Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                    Text = "",
                    Tag = (row, column),
                    TabStop = false,
                    Enabled = false,
                    UseVisualStyleBackColor = false,
                    UseCompatibleTextRendering = true
                };

                cell.FlatAppearance.BorderSize = 0;

                cell.FlatAppearance.MouseOverBackColor =
                    Color.FromArgb(225, 237, 255);

                cell.FlatAppearance.MouseDownBackColor =
                    Color.FromArgb(205, 222, 250);

                cell.Click += Cell_Click;

                _cells[row, column] = cell;

                // TableLayoutPanel nhận column trước, row sau.
                grid.Controls.Add(cell, column, row);
            }
        }

        pnlBoard.Controls.Add(grid);
        grid.ResumeLayout();
    }

    private void ResizeBoard()
    {
        // DisplayRectangle đã tính Padding của vùng chứa.
        Rectangle area = pnlBoardHost.DisplayRectangle;

        int side = Math.Max(
            0,
            Math.Min(area.Width, area.Height));

        pnlBoard.SetBounds(
            area.Left + (area.Width - side) / 2,
            area.Top + (area.Height - side) / 2,
            side,
            side);
    }

    private void Cell_Click(object? sender, EventArgs e)
    {
        if (!_canMove || sender is not Button cell)
            return;

        if (!string.IsNullOrEmpty(cell.Text))
            return;

        if (MoveRequested is null)
            return;

        var (row, column) = ((int, int))cell.Tag!;

        // Chặn bấm thêm trong khi chờ xử lý nước đi.
        SetBoardInteraction(false);

        MoveRequested.Invoke(row, column);
    }

    // Các hàm Display... phải được gọi trên UI thread.

    public void DisplayRoom(
        string playerX,
        string playerO,
        string status)
    {
        lblRoomTitle.Text = "Ván cờ Caro";
        lblRoomSubtitle.Text = $"{playerX} (X)  —  {playerO} (O)";

        lblPlayerX.Text = $"X   {playerX}";
        lblPlayerO.Text = $"O   {playerO}";

        lblRoomStatus.Text = UiText.RoomStatus(status);

        lblRoomStatus.ForeColor = status is "Playing" or "Đang chơi"
            ? Color.FromArgb(22, 163, 74)
            : Color.FromArgb(100, 116, 139);
        lblRoomStatus.BackColor = status switch
        {
            "Playing" or "Đang chơi" => Color.FromArgb(240, 253, 244),
            "Finished" or "Đã kết thúc" => Color.FromArgb(241, 245, 249),
            _ => Color.FromArgb(255, 251, 235)
        };

        btnLeave.Enabled = true;
    }

    public void DisplayTurn(string message, int remainingSeconds)
    {
        remainingSeconds = Math.Max(0, remainingSeconds);

        lblTurn.Text = message;

        int minutes = remainingSeconds / 60;
        int seconds = remainingSeconds % 60;

        lblTime.Text = remainingSeconds > 0
            ? $"{minutes:00}:{seconds:00}"
            : "--:--";
        _turnDeadlineUtc = DateTime.UtcNow.AddSeconds(remainingSeconds);
        if (remainingSeconds > 0)
            _turnTimer.Start();
        else
            _turnTimer.Stop();
    }

    public void DisplayActivePlayer(string symbol)
    {
        if (symbol == "X")
        {
            lblPlayerX.BackColor = Color.FromArgb(37, 99, 235);
            lblPlayerX.ForeColor = Color.White;
            if (!lblPlayerX.Text.StartsWith("▶")) lblPlayerX.Text = "▶ " + lblPlayerX.Text;

            lblPlayerO.BackColor = Color.FromArgb(254, 242, 242);
            lblPlayerO.ForeColor = Color.FromArgb(239, 83, 80);
            lblPlayerO.Text = lblPlayerO.Text.Replace("▶ ", "");
        }
        else if (symbol == "O")
        {
            lblPlayerO.BackColor = Color.FromArgb(239, 83, 80);
            lblPlayerO.ForeColor = Color.White;
            if (!lblPlayerO.Text.StartsWith("▶")) lblPlayerO.Text = "▶ " + lblPlayerO.Text;

            lblPlayerX.BackColor = Color.FromArgb(239, 246, 255);
            lblPlayerX.ForeColor = Color.FromArgb(37, 99, 235);
            lblPlayerX.Text = lblPlayerX.Text.Replace("▶ ", "");
        }
        else
        {
            lblPlayerX.BackColor = Color.FromArgb(239, 246, 255);
            lblPlayerX.ForeColor = Color.FromArgb(37, 99, 235);
            lblPlayerX.Text = lblPlayerX.Text.Replace("▶ ", "");

            lblPlayerO.BackColor = Color.FromArgb(254, 242, 242);
            lblPlayerO.ForeColor = Color.FromArgb(239, 83, 80);
            lblPlayerO.Text = lblPlayerO.Text.Replace("▶ ", "");
        }
    }

    public void DisplaySpectators(int count)
    {
        lblSpectators.Text = $"Khán giả: {count}";
    }

    public void DisplayReadyState(bool isReady, bool canReady)
    {
        btnReady.Text = isReady ? "Đã sẵn sàng" : "Sẵn sàng";
        btnReady.Enabled = canReady && !isReady;
    }

    public void DisplayRematchState(bool canRequest, bool accepted)
    {
        _rematch.Text = accepted ? "Đang chờ đối thủ" : "Chơi lại";
        _rematch.Enabled = canRequest && !accepted;
    }

    public void DisplaySurrenderState(bool canSurrender)
    {
        _surrender.Enabled = canSurrender;
    }

    public void DisplayPendingState()
    {
        ShowActions();
        SetBoardInteraction(false);
    }

    public void DisplayActionMode(GameActionMode mode)
    {
        switch (mode)
        {
            case GameActionMode.WaitingPlayer:
                btnLeave.Text = "Rời phòng";
                btnLeave.Enabled = true;
                ShowActions(btnReady, btnLeave);
                break;
            case GameActionMode.PlayingPlayer:
                _surrender.Enabled = true;
                ShowActions(_surrender);
                break;
            case GameActionMode.FinishedPlayer:
                btnLeave.Text = "Đóng phòng";
                btnLeave.Enabled = true;
                ShowActions(_rematch, btnLeave);
                break;
            case GameActionMode.Spectator:
                btnLeave.Text = "Rời phòng xem";
                btnLeave.Enabled = true;
                ShowActions(btnLeave);
                break;
            default:
                ShowActions();
                break;
        }
    }

    private void ShowActions(params Button[] buttons)
    {
        tblActions.SuspendLayout();
        try
        {
            tblActions.Controls.Clear();
            if (buttons.Length == 1)
            {
                buttons[0].Margin = new Padding(0, 4, 0, 0);
                tblActions.SetColumnSpan(buttons[0], 2);
                tblActions.Controls.Add(buttons[0], 0, 0);
            }
            else if (buttons.Length >= 2)
            {
                buttons[0].Margin = new Padding(0, 4, 5, 0);
                buttons[1].Margin = new Padding(5, 4, 0, 0);
                tblActions.SetColumnSpan(buttons[0], 1);
                tblActions.SetColumnSpan(buttons[1], 1);
                tblActions.Controls.Add(buttons[0], 0, 0);
                tblActions.Controls.Add(buttons[1], 1, 0);
            }
        }
        finally { tblActions.ResumeLayout(performLayout: true); }
    }

    private static void ConfigureActionButton(Button button, string text,
        Color backColor, Color foreColor)
    {
        button.Text = text;
        button.Dock = DockStyle.Fill;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = foreColor;
        button.FlatAppearance.BorderSize = backColor == Color.White ? 1 : 0;
        button.FlatAppearance.MouseOverBackColor = backColor == Color.White
            ? Color.FromArgb(254, 242, 242)
            : Color.FromArgb(29, 78, 216);
        button.FlatAppearance.MouseDownBackColor = backColor == Color.White
            ? Color.FromArgb(254, 226, 226)
            : Color.FromArgb(30, 64, 175);
        button.BackColor = backColor;
        button.ForeColor = foreColor;
        button.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        button.Cursor = Cursors.Hand;
        button.UseVisualStyleBackColor = false;
    }

    public void DisplayHint(string message)
    {
        lblHint.Text = message;
    }

    public void DisplayBoard(string?[,] board, bool canMove)
    {
        if (board.GetLength(0) != BoardSize || board.GetLength(1) != BoardSize)
            throw new ArgumentException("Bàn cờ phải có kích thước 15×15.", nameof(board));

        // Kiểm tra toàn bộ dữ liệu trước khi thay đổi giao diện.
        foreach (string? symbol in board)
        {
            if (symbol is not null and not "" and not "X" and not "O")
            {
                throw new ArgumentException(
                    "Ô cờ chỉ được chứa X, O hoặc để trống.",
                    nameof(board));
            }
        }

        SuspendLayout();
        try
        {
            for (int row = 0; row < BoardSize; row++)
            {
                for (int column = 0; column < BoardSize; column++)
                {
                    Button cell = _cells[row, column];

                    string symbol = board[row, column] ?? "";

                    cell.Text = symbol;

                    cell.ForeColor = symbol == "X"
                        ? Color.FromArgb(37, 99, 235)
                        : Color.FromArgb(239, 83, 80);
                }
            }
        }
        finally { ResumeLayout(performLayout: false); }

        SetBoardInteraction(canMove);
    }

    public void DisplayMove(int row, int column, string symbol, bool canMove)
    {
        if (row is < 0 or >= BoardSize || column is < 0 or >= BoardSize)
            throw new ArgumentOutOfRangeException(nameof(row));
        if (symbol is not "X" and not "O")
            throw new ArgumentException("Quân cờ phải là X hoặc O.", nameof(symbol));
        var cell = _cells[row, column];
        cell.Text = symbol;
        cell.ForeColor = symbol == "X"
            ? Color.FromArgb(37, 99, 235)
            : Color.FromArgb(239, 83, 80);
        SetBoardInteraction(canMove);
    }

    public void SetBoardInteraction(bool canMove)
    {
        _canMove = canMove;

        foreach (Button cell in _cells)
        {
            // Giữ các ô đã có quân ở trạng thái Enabled để
            // WinForms hiển thị đúng màu X/O. Cell_Click chặn chúng.
            bool occupied = !string.IsNullOrEmpty(cell.Text);

            cell.Enabled = occupied || canMove;
            cell.Cursor = canMove && !occupied
                ? Cursors.Hand
                : Cursors.Default;
        }
    }

    private void UpdateCountdown()
    {
        var remaining = Math.Max(0, (int)Math.Ceiling((_turnDeadlineUtc - DateTime.UtcNow).TotalSeconds));
        var minutes = remaining / 60;
        var seconds = remaining % 60;
        lblTime.Text = $"{minutes:00}:{seconds:00}";
        if (remaining == 0) _turnTimer.Stop();
    }

}

public enum GameActionMode
{
    Pending,
    WaitingPlayer,
    PlayingPlayer,
    FinishedPlayer,
    Spectator
}
