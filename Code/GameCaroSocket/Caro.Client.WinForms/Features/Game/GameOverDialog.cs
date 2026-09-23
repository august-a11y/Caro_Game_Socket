using System;
using System.Drawing;
using System.Windows.Forms;

namespace Caro.Client.WinForms.Features.Game;

public sealed class GameOverDialog : Form
{
    private Label lblTitle = null!;
    private Label lblReason = null!;
    private Button btnClose = null!;

    public GameOverDialog(string title, string reason, bool isWin)
    {
        InitializeComponent();
        
        lblTitle.Text = title;
        lblReason.Text = reason;

        if (isWin)
        {
            lblTitle.ForeColor = Color.FromArgb(34, 197, 94); // Green
        }
        else if (title.Contains("HÒA") || title.Contains("KẾT THÚC"))
        {
            lblTitle.ForeColor = Color.FromArgb(234, 179, 8); // Yellow
        }
        else
        {
            lblTitle.ForeColor = Color.FromArgb(239, 68, 68); // Red
        }
    }

    private void InitializeComponent()
    {
        this.lblTitle = new Label();
        this.lblReason = new Label();
        this.btnClose = new Button();
        
        this.SuspendLayout();
        
        // 
        // lblTitle
        // 
        this.lblTitle.Dock = DockStyle.Top;
        this.lblTitle.Font = new Font("Segoe UI", 24F, FontStyle.Bold);
        this.lblTitle.TextAlign = ContentAlignment.BottomCenter;
        this.lblTitle.Height = 100;
        
        // 
        // lblReason
        // 
        this.lblReason.Dock = DockStyle.Top;
        this.lblReason.Font = new Font("Segoe UI", 12F);
        this.lblReason.ForeColor = Color.DimGray;
        this.lblReason.TextAlign = ContentAlignment.MiddleCenter;
        this.lblReason.Height = 50;
        
        // 
        // btnClose
        // 
        this.btnClose.Text = "Đóng";
        this.btnClose.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        this.btnClose.BackColor = Color.FromArgb(37, 99, 235);
        this.btnClose.ForeColor = Color.White;
        this.btnClose.FlatStyle = FlatStyle.Flat;
        this.btnClose.FlatAppearance.BorderSize = 0;
        this.btnClose.Cursor = Cursors.Hand;
        this.btnClose.Size = new Size(150, 45);
        this.btnClose.Location = new Point(125, 170);
        this.btnClose.Click += (s, e) => this.Close();
        
        // 
        // GameOverDialog
        // 
        this.ClientSize = new Size(400, 250);
        this.Controls.Add(this.btnClose);
        this.Controls.Add(this.lblReason);
        this.Controls.Add(this.lblTitle);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.ShowIcon = false;
        this.ShowInTaskbar = false;
        this.StartPosition = FormStartPosition.CenterParent;
        this.Text = "Kết quả trận đấu";
        this.BackColor = Color.White;
        
        this.ResumeLayout(false);
    }
}
