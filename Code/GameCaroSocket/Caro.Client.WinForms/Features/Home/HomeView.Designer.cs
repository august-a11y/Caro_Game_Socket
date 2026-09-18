using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Caro.Client.WinForms;

partial class HomeView
{
    private IContainer? components = null;

    private TableLayoutPanel tblRoot;
    private TableLayoutPanel tblCards;
    private TableLayoutPanel tblSteps;

    private Panel pnlHeading;
    private Panel pnlPlayers;
    private Panel pnlMatches;
    private Panel pnlInvitations;
    private Panel pnlInstructions;

    private Label lblTitle;
    private Label lblGreeting;

    private Label lblOnlineCount;
    private Label lblOnlineCaption;

    private Label lblMatchCount;
    private Label lblMatchCaption;

    private Label lblInvitationCount;
    private Label lblInvitationCaption;

    private Label lblInstructionsTitle;
    private Label lblStepOne;
    private Label lblStepTwo;
    private Label lblStepThree;

    private Button btnViewPlayers;
    private Button btnViewMatches;
    private Button btnViewInvitations;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        tblRoot = new TableLayoutPanel();
        tblCards = new TableLayoutPanel();
        tblSteps = new TableLayoutPanel();

        pnlHeading = new Panel();
        pnlPlayers = new Panel();
        pnlMatches = new Panel();
        pnlInvitations = new Panel();
        pnlInstructions = new Panel();

        lblTitle = new Label();
        lblGreeting = new Label();

        lblOnlineCount = new Label();
        lblOnlineCaption = new Label();

        lblMatchCount = new Label();
        lblMatchCaption = new Label();

        lblInvitationCount = new Label();
        lblInvitationCaption = new Label();

        lblInstructionsTitle = new Label();
        lblStepOne = new Label();
        lblStepTwo = new Label();
        lblStepThree = new Label();

        btnViewPlayers = new Button();
        btnViewMatches = new Button();
        btnViewInvitations = new Button();

        tblRoot.SuspendLayout();
        tblCards.SuspendLayout();
        tblSteps.SuspendLayout();

        pnlHeading.SuspendLayout();
        pnlPlayers.SuspendLayout();
        pnlMatches.SuspendLayout();
        pnlInvitations.SuspendLayout();
        pnlInstructions.SuspendLayout();

        SuspendLayout();

        // tblRoot
        tblRoot.ColumnCount = 1;
        tblRoot.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100F));

        tblRoot.RowCount = 3;
        tblRoot.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 84F));
        tblRoot.RowStyles.Add(
            new RowStyle(SizeType.Percent, 60F));
        tblRoot.RowStyles.Add(
            new RowStyle(SizeType.Percent, 40F));

        tblRoot.Controls.Add(pnlHeading, 0, 0);
        tblRoot.Controls.Add(tblCards, 0, 1);
        tblRoot.Controls.Add(pnlInstructions, 0, 2);

        tblRoot.Dock = DockStyle.Fill;
        tblRoot.Margin = new Padding(0);
        tblRoot.Name = "tblRoot";
        tblRoot.TabIndex = 0;

        // pnlHeading
        pnlHeading.Controls.Add(lblTitle);
        pnlHeading.Controls.Add(lblGreeting);
        pnlHeading.Dock = DockStyle.Fill;
        pnlHeading.Margin = new Padding(0);
        pnlHeading.Name = "pnlHeading";

        // lblTitle
        lblTitle.AutoSize = true;
        lblTitle.Font = new Font(
            "Segoe UI", 24F, FontStyle.Bold);
        lblTitle.ForeColor = Color.FromArgb(23, 43, 77);
        lblTitle.Location = new Point(0, 0);
        lblTitle.Name = "lblTitle";
        lblTitle.Text = "Trang chủ";

        // lblGreeting
        lblGreeting.Anchor =
            AnchorStyles.Top |
            AnchorStyles.Left |
            AnchorStyles.Right;

        lblGreeting.AutoEllipsis = true;
        lblGreeting.Font = new Font("Segoe UI", 11F);
        lblGreeting.ForeColor = Color.FromArgb(100, 116, 139);
        lblGreeting.Location = new Point(3, 49);
        lblGreeting.Name = "lblGreeting";
        lblGreeting.Size = new Size(930, 26);
        lblGreeting.Text = "Chào bạn, sẵn sàng cho một ván Caro?";

        // tblCards
        tblCards.ColumnCount = 3;
        tblCards.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 33.33333F));
        tblCards.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 33.33333F));
        tblCards.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 33.33334F));

        tblCards.RowCount = 1;
        tblCards.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100F));

        tblCards.Controls.Add(pnlPlayers, 0, 0);
        tblCards.Controls.Add(pnlMatches, 1, 0);
        tblCards.Controls.Add(pnlInvitations, 2, 0);

        tblCards.Dock = DockStyle.Fill;
        tblCards.Margin = new Padding(0, 0, 0, 16);
        tblCards.Name = "tblCards";
        tblCards.TabIndex = 0;

        // pnlPlayers
        pnlPlayers.BackColor = Color.White;
        pnlPlayers.BorderStyle = BorderStyle.FixedSingle;
        pnlPlayers.Controls.Add(btnViewPlayers);
        pnlPlayers.Controls.Add(lblOnlineCaption);
        pnlPlayers.Controls.Add(lblOnlineCount);
        pnlPlayers.Dock = DockStyle.Fill;
        pnlPlayers.Margin = new Padding(0, 0, 8, 0);
        pnlPlayers.Name = "pnlPlayers";
        pnlPlayers.Padding = new Padding(12);
        pnlPlayers.TabIndex = 0;

        // lblOnlineCount
        lblOnlineCount.Dock = DockStyle.Top;
        lblOnlineCount.Font = new Font(
            "Segoe UI", 34F, FontStyle.Bold);
        lblOnlineCount.ForeColor = Color.FromArgb(23, 43, 77);
        lblOnlineCount.Name = "lblOnlineCount";
        lblOnlineCount.Size = new Size(270, 56);
        lblOnlineCount.Text = "0";
        lblOnlineCount.TextAlign = ContentAlignment.MiddleCenter;

        // lblOnlineCaption
        lblOnlineCaption.Dock = DockStyle.Top;
        lblOnlineCaption.Font = new Font("Segoe UI", 11F);
        lblOnlineCaption.ForeColor = Color.FromArgb(100, 116, 139);
        lblOnlineCaption.Name = "lblOnlineCaption";
        lblOnlineCaption.Size = new Size(270, 30);
        lblOnlineCaption.Text = "Người chơi trực tuyến";
        lblOnlineCaption.TextAlign = ContentAlignment.MiddleCenter;

        // btnViewPlayers
        btnViewPlayers.BackColor = Color.FromArgb(37, 99, 235);
        btnViewPlayers.Cursor = Cursors.Hand;
        btnViewPlayers.Dock = DockStyle.Bottom;
        btnViewPlayers.FlatAppearance.BorderSize = 0;
        btnViewPlayers.FlatStyle = FlatStyle.Flat;
        btnViewPlayers.Font = new Font(
            "Segoe UI", 10F, FontStyle.Bold);
        btnViewPlayers.ForeColor = Color.White;
        btnViewPlayers.Name = "btnViewPlayers";
        btnViewPlayers.Size = new Size(270, 38);
        btnViewPlayers.TabIndex = 0;
        btnViewPlayers.Text = "Xem người chơi";
        btnViewPlayers.UseVisualStyleBackColor = false;

        // pnlMatches
        pnlMatches.BackColor = Color.White;
        pnlMatches.BorderStyle = BorderStyle.FixedSingle;
        pnlMatches.Controls.Add(btnViewMatches);
        pnlMatches.Controls.Add(lblMatchCaption);
        pnlMatches.Controls.Add(lblMatchCount);
        pnlMatches.Dock = DockStyle.Fill;
        pnlMatches.Margin = new Padding(8, 0, 8, 0);
        pnlMatches.Name = "pnlMatches";
        pnlMatches.Padding = new Padding(12);
        pnlMatches.TabIndex = 1;

        // lblMatchCount
        lblMatchCount.Dock = DockStyle.Top;
        lblMatchCount.Font = new Font(
            "Segoe UI", 34F, FontStyle.Bold);
        lblMatchCount.ForeColor = Color.FromArgb(23, 43, 77);
        lblMatchCount.Name = "lblMatchCount";
        lblMatchCount.Size = new Size(260, 56);
        lblMatchCount.Text = "0";
        lblMatchCount.TextAlign = ContentAlignment.MiddleCenter;

        // lblMatchCaption
        lblMatchCaption.Dock = DockStyle.Top;
        lblMatchCaption.Font = new Font("Segoe UI", 11F);
        lblMatchCaption.ForeColor = Color.FromArgb(100, 116, 139);
        lblMatchCaption.Name = "lblMatchCaption";
        lblMatchCaption.Size = new Size(260, 30);
        lblMatchCaption.Text = "Trận đang chơi";
        lblMatchCaption.TextAlign = ContentAlignment.MiddleCenter;

        // btnViewMatches
        btnViewMatches.BackColor = Color.White;
        btnViewMatches.Cursor = Cursors.Hand;
        btnViewMatches.Dock = DockStyle.Bottom;
        btnViewMatches.FlatAppearance.BorderColor =
            Color.FromArgb(37, 99, 235);
        btnViewMatches.FlatStyle = FlatStyle.Flat;
        btnViewMatches.Font = new Font(
            "Segoe UI", 10F, FontStyle.Bold);
        btnViewMatches.ForeColor = Color.FromArgb(37, 99, 235);
        btnViewMatches.Name = "btnViewMatches";
        btnViewMatches.Size = new Size(260, 38);
        btnViewMatches.TabIndex = 0;
        btnViewMatches.Text = "Xem trận đấu";
        btnViewMatches.UseVisualStyleBackColor = false;

        // pnlInvitations
        pnlInvitations.BackColor = Color.White;
        pnlInvitations.BorderStyle = BorderStyle.FixedSingle;
        pnlInvitations.Controls.Add(btnViewInvitations);
        pnlInvitations.Controls.Add(lblInvitationCaption);
        pnlInvitations.Controls.Add(lblInvitationCount);
        pnlInvitations.Dock = DockStyle.Fill;
        pnlInvitations.Margin = new Padding(8, 0, 0, 0);
        pnlInvitations.Name = "pnlInvitations";
        pnlInvitations.Padding = new Padding(12);
        pnlInvitations.TabIndex = 2;

        // lblInvitationCount
        lblInvitationCount.Dock = DockStyle.Top;
        lblInvitationCount.Font = new Font(
            "Segoe UI", 34F, FontStyle.Bold);
        lblInvitationCount.ForeColor = Color.FromArgb(23, 43, 77);
        lblInvitationCount.Name = "lblInvitationCount";
        lblInvitationCount.Size = new Size(270, 56);
        lblInvitationCount.Text = "0";
        lblInvitationCount.TextAlign = ContentAlignment.MiddleCenter;

        // lblInvitationCaption
        lblInvitationCaption.Dock = DockStyle.Top;
        lblInvitationCaption.Font = new Font("Segoe UI", 11F);
        lblInvitationCaption.ForeColor = Color.FromArgb(100, 116, 139);
        lblInvitationCaption.Name = "lblInvitationCaption";
        lblInvitationCaption.Size = new Size(270, 30);
        lblInvitationCaption.Text = "Lời mời mới";
        lblInvitationCaption.TextAlign = ContentAlignment.MiddleCenter;

        // btnViewInvitations
        btnViewInvitations.BackColor = Color.White;
        btnViewInvitations.Cursor = Cursors.Hand;
        btnViewInvitations.Dock = DockStyle.Bottom;
        btnViewInvitations.FlatAppearance.BorderColor =
            Color.FromArgb(37, 99, 235);
        btnViewInvitations.FlatStyle = FlatStyle.Flat;
        btnViewInvitations.Font = new Font(
            "Segoe UI", 10F, FontStyle.Bold);
        btnViewInvitations.ForeColor = Color.FromArgb(37, 99, 235);
        btnViewInvitations.Name = "btnViewInvitations";
        btnViewInvitations.Size = new Size(270, 38);
        btnViewInvitations.TabIndex = 0;
        btnViewInvitations.Text = "Xem lời mời";
        btnViewInvitations.UseVisualStyleBackColor = false;

        // pnlInstructions
        pnlInstructions.BackColor = Color.White;
        pnlInstructions.BorderStyle = BorderStyle.FixedSingle;
        pnlInstructions.Controls.Add(tblSteps);
        pnlInstructions.Controls.Add(lblInstructionsTitle);
        pnlInstructions.Dock = DockStyle.Fill;
        pnlInstructions.Margin = new Padding(0);
        pnlInstructions.Name = "pnlInstructions";
        pnlInstructions.Padding = new Padding(14);

        // lblInstructionsTitle
        lblInstructionsTitle.Dock = DockStyle.Top;
        lblInstructionsTitle.Font = new Font(
            "Segoe UI", 15F, FontStyle.Bold);
        lblInstructionsTitle.ForeColor = Color.FromArgb(23, 43, 77);
        lblInstructionsTitle.Name = "lblInstructionsTitle";
        lblInstructionsTitle.Size = new Size(900, 34);
        lblInstructionsTitle.Text = "Bắt đầu chơi";

        // tblSteps
        tblSteps.ColumnCount = 3;
        tblSteps.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 33.33333F));
        tblSteps.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 33.33333F));
        tblSteps.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 33.33334F));

        tblSteps.RowCount = 1;
        tblSteps.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100F));

        tblSteps.Controls.Add(lblStepOne, 0, 0);
        tblSteps.Controls.Add(lblStepTwo, 1, 0);
        tblSteps.Controls.Add(lblStepThree, 2, 0);

        tblSteps.Dock = DockStyle.Fill;
        tblSteps.Margin = new Padding(0);
        tblSteps.Name = "tblSteps";

        // lblStepOne
        lblStepOne.Dock = DockStyle.Fill;
        lblStepOne.Font = new Font("Segoe UI", 10F);
        lblStepOne.ForeColor = Color.FromArgb(100, 116, 139);
        lblStepOne.Margin = new Padding(0, 0, 16, 0);
        lblStepOne.Name = "lblStepOne";
        lblStepOne.Text =
            "1. Chọn đối thủ\r\n" +
            "Chọn một người chơi đang rảnh.";

        // lblStepTwo
        lblStepTwo.Dock = DockStyle.Fill;
        lblStepTwo.Font = new Font("Segoe UI", 10F);
        lblStepTwo.ForeColor = Color.FromArgb(100, 116, 139);
        lblStepTwo.Margin = new Padding(8, 0, 16, 0);
        lblStepTwo.Name = "lblStepTwo";
        lblStepTwo.Text =
            "2. Gửi lời mời\r\n" +
            "Chờ đối thủ chấp nhận.";

        // lblStepThree
        lblStepThree.Dock = DockStyle.Fill;
        lblStepThree.Font = new Font("Segoe UI", 10F);
        lblStepThree.ForeColor = Color.FromArgb(100, 116, 139);
        lblStepThree.Margin = new Padding(8, 0, 0, 0);
        lblStepThree.Name = "lblStepThree";
        lblStepThree.Text =
            "3. Sẵn sàng chơi\r\n" +
            "Nhấn Sẵn sàng để bắt đầu trận.";

        // HomeView
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(245, 247, 251);
        Controls.Add(tblRoot);
        Font = new Font("Segoe UI", 10F);
        Margin = new Padding(0);
        Name = "HomeView";
        Size = new Size(942, 527);

        tblRoot.ResumeLayout(false);
        tblCards.ResumeLayout(false);
        tblSteps.ResumeLayout(false);

        pnlHeading.ResumeLayout(false);
        pnlHeading.PerformLayout();
        pnlPlayers.ResumeLayout(false);
        pnlMatches.ResumeLayout(false);
        pnlInvitations.ResumeLayout(false);
        pnlInstructions.ResumeLayout(false);

        ResumeLayout(false);
    }
}
