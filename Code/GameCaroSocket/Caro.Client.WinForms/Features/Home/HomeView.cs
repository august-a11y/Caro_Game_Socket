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