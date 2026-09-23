using System;
using System.Windows.Forms;

namespace Caro.Client.WinForms;

public partial class HomeView : UserControl
{
    public event Action? ViewPlayersRequested;
    public event Action? ViewMatchesRequested;
    public event Action? ViewInvitationsRequested;

    public HomeView()
    {
        InitializeComponent();

        btnViewPlayers.Click += (_, _) =>
            ViewPlayersRequested?.Invoke();

        btnViewMatches.Click += (_, _) =>
            ViewMatchesRequested?.Invoke();

        btnViewInvitations.Click += (_, _) =>
            ViewInvitationsRequested?.Invoke();

        // Biến các đoạn text hướng dẫn thành nút bấm (usable)
        ConfigureClickableLabel(lblStepOne, () => ViewPlayersRequested?.Invoke());
        ConfigureClickableLabel(lblStepTwo, () => ViewInvitationsRequested?.Invoke());
        // Bước 3 chỉ là thông tin, nhưng cho click vào mục Lời mời luôn để đồng bộ
        ConfigureClickableLabel(lblStepThree, () => ViewInvitationsRequested?.Invoke());
    }

    private void ConfigureClickableLabel(Label lbl, Action onClick)
    {
        lbl.Cursor = Cursors.Hand;
        lbl.MouseEnter += (_, _) => lbl.ForeColor = System.Drawing.Color.FromArgb(37, 99, 235); // Đổi màu xanh khi hover
        lbl.MouseLeave += (_, _) => lbl.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139); // Trả lại màu xám
        lbl.Click += (_, _) => onClick();
    }

    // Các hàm cập nhật giao diện cần được gọi trên UI thread.
    public void DisplayNickname(string nickname)
    {
        lblGreeting.Text =
            $"Chào {nickname}, sẵn sàng cho một ván Caro?";
    }

    public void DisplayStatistics(
        int onlinePlayers,
        int activeMatches,
        int pendingInvitations)
    {
        lblOnlineCount.Text = onlinePlayers.ToString();
        lblMatchCount.Text = activeMatches.ToString();
        lblInvitationCount.Text = pendingInvitations.ToString();
    }
}