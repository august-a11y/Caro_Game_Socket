using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Caro.Client.WinForms;

public partial class InvitationsView : UserControl
{
    public event Action? RefreshRequested;
    public event Action<Guid, bool>? ChallengeResponseRequested;
    public event Action<Guid>? ChallengeCancelRequested;

    public InvitationsView()
    {
        InitializeComponent();
        btnRefresh.Click += (_, _) => RefreshRequested?.Invoke();
        dgvReceived.CellContentClick += ReceivedCellContentClick;
        dgvSent.CellContentClick += SentCellContentClick;
    }

    public void DisplayInvitations(IEnumerable<InvitationRow> invitations)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => DisplayInvitations(invitations));
            return;
        }

        var rows = invitations.ToArray();
        dgvReceived.Rows.Clear();
        dgvSent.Rows.Clear();
        foreach (var invitation in rows.Where(row => row.Incoming))
        {
            var row = dgvReceived.Rows[dgvReceived.Rows.Add(
                invitation.PeerName, FormatExpiry(invitation.ExpiresAt),
                invitation.Status, "Chấp nhận", "Từ chối")];
            row.Tag = invitation;
            if (!invitation.IsPending || invitation.IsBusy)
            {
                var label = invitation.IsBusy ? "Đang xử lý..." : "—";
                ReplaceActionWithText(row, colAccept.Index, label);
                ReplaceActionWithText(row, colDecline.Index, "—");
            }
        }
        foreach (var invitation in rows.Where(row => !row.Incoming))
        {
            var row = dgvSent.Rows[dgvSent.Rows.Add(
                invitation.PeerName, FormatExpiry(invitation.ExpiresAt),
                invitation.Status, "Hủy lời mời")];
            row.Tag = invitation;
            if (!invitation.IsPending || invitation.IsBusy)
                ReplaceActionWithText(row, colCancel.Index,
                    invitation.IsBusy ? "Đang xử lý..." : "—");
        }
        lblStatus.Text =
            $"{dgvReceived.Rows.Count} lời mời đã nhận • {dgvSent.Rows.Count} lời mời đã gửi";
    }

    public void DisplayStatus(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => DisplayStatus(message));
            return;
        }
        lblStatus.ForeColor = Color.FromArgb(100, 116, 139);
        lblStatus.Text = message;
    }

    public void DisplayError(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => DisplayError(message));
            return;
        }
        lblStatus.ForeColor = Color.Firebrick;
        lblStatus.Text = message;
    }

    private void ReceivedCellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || dgvReceived.Rows[e.RowIndex].Tag is not InvitationRow invitation ||
            !invitation.IsPending) return;
        if (e.ColumnIndex == colAccept.Index) ChallengeResponseRequested?.Invoke(invitation.ChallengeId, true);
        else if (e.ColumnIndex == colDecline.Index) ChallengeResponseRequested?.Invoke(invitation.ChallengeId, false);
    }

    private void SentCellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0 && e.ColumnIndex == colCancel.Index &&
            dgvSent.Rows[e.RowIndex].Tag is InvitationRow invitation && invitation.IsPending)
            ChallengeCancelRequested?.Invoke(invitation.ChallengeId);
    }

    private static string FormatExpiry(DateTime expiresAt)
    {
        var remaining = expiresAt - DateTime.UtcNow;
        return remaining <= TimeSpan.Zero ? "Đã hết hạn" : $"Còn {Math.Ceiling(remaining.TotalSeconds):0}s";
    }

    private static void ReplaceActionWithText(DataGridViewRow row, int columnIndex, string text)
    {
        row.Cells[columnIndex] = new DataGridViewTextBoxCell
        {
            Value = text,
            Style = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(100, 116, 139),
                SelectionBackColor = Color.FromArgb(241, 245, 249),
                SelectionForeColor = Color.FromArgb(100, 116, 139)
            }
        };
        row.Cells[columnIndex].ReadOnly = true;
    }
}

public sealed record InvitationRow(
    Guid ChallengeId,
    string PeerName,
    DateTime ExpiresAt,
    string Status,
    bool Incoming,
    bool IsPending,
    bool IsBusy);
