using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Caro.Client.WinForms.Core;

namespace Caro.Client.WinForms;

public partial class MatchesView : UserControl
{
    private List<MatchRow> _matches = new();
    private bool _isLoading;

    public event Action? RefreshRequested;
    public event Action<Guid>? WatchMatchRequested;

    public MatchesView()
    {
        InitializeComponent();

        btnRefresh.Click += (_, _) =>
            RefreshRequested?.Invoke();

        dgvMatches.CellContentClick += Matches_CellContentClick;
    }

    // Các hàm Display... cần được gọi trên UI thread.
    public void DisplayMatches(IEnumerable<MatchRow> matches)
    {
        _matches = matches.ToList();

        dgvMatches.Rows.Clear();
        foreach (var match in _matches)
        {
            var row = dgvMatches.Rows[dgvMatches.Rows.Add(
                match.PlayerX,
                match.PlayerO,
                match.Spectators,
                UiText.RoomStatus(match.Status),
                "Xem")];
            row.Tag = match.RoomId;
        }

        SetLoading(false);
        lblStatus.ForeColor = Color.FromArgb(100, 116, 139);
        lblStatus.Text = _matches.Count == 0
            ? "Chưa có trận đấu đang hoạt động."
            : $"{_matches.Count} trận đấu đang hoạt động.";
    }

    public void DisplayLoading()
    {
        SetLoading(true);

        lblStatus.ForeColor = Color.FromArgb(100, 116, 139);
        lblStatus.Text = "Đang tải danh sách trận đấu...";
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

        dgvMatches.Enabled = !loading;
    }

    

    private void Matches_CellContentClick(
        object? sender,
        DataGridViewCellEventArgs e)
    {
        if (_isLoading || e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        if (e.ColumnIndex != colWatch.Index)
            return;

        if (dgvMatches.Rows[e.RowIndex].Tag is Guid roomId)
        {
            WatchMatchRequested?.Invoke(roomId);
        }
    }
}

// RoomId chỉ dùng nội bộ khi gửi yêu cầu xem trận, không hiển thị trên giao diện.
public sealed record MatchRow(
    Guid RoomId,
    string PlayerX,
    string PlayerO,
    int Spectators,
    string Status);
