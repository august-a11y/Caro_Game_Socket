using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Caro.Client.WinForms;

// Model dành cho giao diện.
// Thành viên xử lý dữ liệu ánh xạ DTO server sang model này.
public sealed record OnlinePlayerRow(
    Guid PlayerId,
    string Nickname,
    string Status,
    int Wins,
    int Losses,
    bool IsSelf = false);

public partial class OnlinePlayersView : UserControl
{
    private List<OnlinePlayerRow> _players = new();
    private bool _isLoading;

    public event Action? RefreshRequested;
    public event Action<Guid>? ChallengeRequested;

    public OnlinePlayersView()
    {
        InitializeComponent();
        // PlayerDto currently supplies only the player's identity, name and status.
        colWins.Visible = false;
        colLosses.Visible = false;
        colChallenge.Visible = true;
        lblDescription.Text = "Danh sách người chơi đang kết nối. Nhấn Thách đấu để gửi lời mời.";


        btnRefresh.Click += (_, _) =>
            RefreshRequested?.Invoke();

        dgvPlayers.CellContentClick += Players_CellContentClick;
    }

    // Các hàm Display... cần được gọi trên UI thread.
    public void DisplayPlayers(IEnumerable<OnlinePlayerRow> players)
    {
        _players = players.ToList();
        var selected = (dgvPlayers.CurrentRow?.Tag as OnlinePlayerRow)?.PlayerId;
        dgvPlayers.Rows.Clear();
        foreach (var player in _players)
        {
            var canChallenge = !player.IsSelf && player.Status == "Rảnh";
            int index = dgvPlayers.Rows.Add(player.IsSelf ? $"{player.Nickname} (Bạn)" : player.Nickname,
                player.Status, player.Wins, player.Losses,
                canChallenge ? "Thách đấu" : player.IsSelf ? "Bạn" : "Đang bận");
            var row = dgvPlayers.Rows[index];
            row.Tag = player;
            if (player.PlayerId == selected) dgvPlayers.CurrentCell = row.Cells[colNickname.Index];
            if (canChallenge)
            {
                row.Cells[colChallenge.Index].Style.BackColor = Color.FromArgb(37, 99, 235);
                row.Cells[colChallenge.Index].Style.ForeColor = Color.White;
                row.Cells[colChallenge.Index].Style.SelectionBackColor = Color.FromArgb(29, 78, 216);
                row.Cells[colChallenge.Index].Style.SelectionForeColor = Color.White;
            }
            else
            {
                row.Cells[colChallenge.Index] = new DataGridViewTextBoxCell
                {
                    Value = player.IsSelf ? "Tài khoản của bạn" : "Đang bận",
                    Style = new DataGridViewCellStyle
                    {
                        Alignment = DataGridViewContentAlignment.MiddleCenter,
                        ForeColor = Color.FromArgb(100, 116, 139)
                    }
                };
                row.Cells[colChallenge.Index].ReadOnly = true;
            }
        }
        SetLoading(false);
        lblStatus.ForeColor = Color.FromArgb(100, 116, 139);
        lblStatus.Text = _players.Count == 0 ? "Chưa có người chơi trực tuyến."
            : $"{_players.Count} người chơi trực tuyến.";
    }

    public void DisplayLoading()
    {
        SetLoading(true);

        lblStatus.ForeColor = Color.FromArgb(100, 116, 139);
        lblStatus.Text = "Đang tải danh sách người chơi...";
    }

    public void DisplayError(string message)
    {
        SetLoading(false);

        lblStatus.ForeColor = Color.Firebrick;
        lblStatus.Text = message;
    }

    private void SetLoading(bool loading)
    {
        _isLoading = loading;

        btnRefresh.Enabled = !loading;
        dgvPlayers.Enabled = !loading;
    }

 

    private void Players_CellContentClick(
        object? sender,
        DataGridViewCellEventArgs e)
    {
        if (_isLoading || e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        if (e.ColumnIndex != colChallenge.Index)
            return;

        if (dgvPlayers.Rows[e.RowIndex].Tag
            is not OnlinePlayerRow player)
        {
            return;
        }

        // DataGridViewButtonColumn không có Enabled cho từng ô.
        // Chặn thao tác với người đang bận tại đây.
        if (player.IsSelf || player.Status != "Rảnh")
            return;

        ChallengeRequested?.Invoke(player.PlayerId);
    }
}
