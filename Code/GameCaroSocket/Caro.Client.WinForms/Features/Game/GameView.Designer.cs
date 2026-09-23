using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Caro.Client.WinForms;

partial class GameView
{
    private IContainer? components = null;

    private TableLayoutPanel tblRoot;
    private TableLayoutPanel tblBody;
    private TableLayoutPanel tblInformation;
    private TableLayoutPanel tblActions;

    private Panel pnlHeading;
    private Panel pnlBoardHost;
    private Panel pnlBoard;
    private Panel pnlInformation;

    private Label lblRoomTitle;
    private Label lblRoomSubtitle;
    private Label lblRoomStatus;

    private Label lblInformationTitle;
    private Label lblPlayerX;
    private Label lblPlayerO;
    private Label lblTurn;
    private Label lblTime;
    private Label lblTimeCaption;
    private Label lblSpectators;
    private Label lblHint;

    private Button btnReady;
    private Button btnLeave;

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
        tblBody = new TableLayoutPanel();
        tblInformation = new TableLayoutPanel();
        tblActions = new TableLayoutPanel();

        pnlHeading = new Panel();
        pnlBoardHost = new Panel();
        pnlBoard = new Panel();
        pnlInformation = new Panel();

        lblRoomTitle = new Label();
        lblRoomSubtitle = new Label();
        lblRoomStatus = new Label();

        lblInformationTitle = new Label();
        lblPlayerX = new Label();
        lblPlayerO = new Label();
        lblTurn = new Label();
        lblTime = new Label();
        lblTimeCaption = new Label();
        lblSpectators = new Label();
        lblHint = new Label();

        btnReady = new Button();
        btnLeave = new Button();

        tblRoot.SuspendLayout();
        tblBody.SuspendLayout();
        tblInformation.SuspendLayout();
        tblActions.SuspendLayout();

        pnlHeading.SuspendLayout();
        pnlBoardHost.SuspendLayout();
        pnlInformation.SuspendLayout();

        SuspendLayout();

        // tblRoot
        tblRoot.ColumnCount = 1;
        tblRoot.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100F));

        tblRoot.RowCount = 3;
        tblRoot.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 82F));
        tblRoot.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100F));
        tblRoot.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 42F));

        tblRoot.Controls.Add(pnlHeading, 0, 0);
        tblRoot.Controls.Add(tblBody, 0, 1);
        tblRoot.Controls.Add(lblHint, 0, 2);

        tblRoot.Dock = DockStyle.Fill;
        tblRoot.Margin = new Padding(0);
        tblRoot.Name = "tblRoot";
        tblRoot.Size = new Size(942, 527);
        tblRoot.TabIndex = 0;

        // pnlHeading
        pnlHeading.Controls.Add(lblRoomTitle);
        pnlHeading.Controls.Add(lblRoomSubtitle);
        pnlHeading.Controls.Add(lblRoomStatus);
        pnlHeading.Dock = DockStyle.Fill;
        pnlHeading.Margin = new Padding(0);
        pnlHeading.Name = "pnlHeading";
        pnlHeading.Padding = new Padding(2, 0, 2, 8);
        pnlHeading.Size = new Size(942, 82);

        // lblRoomTitle
        lblRoomTitle.Anchor =
            AnchorStyles.Top |
            AnchorStyles.Left |
            AnchorStyles.Right;

        lblRoomTitle.AutoEllipsis = true;
        lblRoomTitle.Font = new Font(
            "Segoe UI", 24F, FontStyle.Bold);
        lblRoomTitle.ForeColor = Color.FromArgb(23, 43, 77);
        lblRoomTitle.Location = new Point(0, 0);
        lblRoomTitle.Name = "lblRoomTitle";
        lblRoomTitle.Size = new Size(680, 46);
        lblRoomTitle.Text = "Phòng chơi";

        // lblRoomSubtitle
        lblRoomSubtitle.Anchor =
            AnchorStyles.Top |
            AnchorStyles.Left |
            AnchorStyles.Right;

        lblRoomSubtitle.AutoEllipsis = true;
        lblRoomSubtitle.Font = new Font("Segoe UI", 10F);
        lblRoomSubtitle.ForeColor = Color.FromArgb(100, 116, 139);
        lblRoomSubtitle.Location = new Point(3, 50);
        lblRoomSubtitle.Name = "lblRoomSubtitle";
        lblRoomSubtitle.Size = new Size(700, 24);
        lblRoomSubtitle.Text = "Chưa tham gia phòng";

        // lblRoomStatus
        lblRoomStatus.Anchor =
            AnchorStyles.Top | AnchorStyles.Right;
        lblRoomStatus.Font = new Font(
            "Segoe UI", 10F, FontStyle.Bold);
        lblRoomStatus.ForeColor = Color.FromArgb(100, 116, 139);
        lblRoomStatus.BackColor = Color.FromArgb(241, 245, 249);
        lblRoomStatus.Location = new Point(752, 10);
        lblRoomStatus.Name = "lblRoomStatus";
        lblRoomStatus.Padding = new Padding(10, 0, 10, 0);
        lblRoomStatus.Size = new Size(188, 34);
        lblRoomStatus.Text = "Chưa có trận";
        lblRoomStatus.TextAlign = ContentAlignment.MiddleRight;

        // tblBody
        tblBody.ColumnCount = 2;
        tblBody.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 70F));
        tblBody.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 30F));

        tblBody.RowCount = 1;
        tblBody.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100F));

        tblBody.Controls.Add(pnlBoardHost, 0, 0);
        tblBody.Controls.Add(pnlInformation, 1, 0);

        tblBody.Dock = DockStyle.Fill;
        tblBody.Margin = new Padding(0);
        tblBody.Name = "tblBody";
        tblBody.TabIndex = 0;

        // pnlBoardHost
        pnlBoardHost.BackColor = Color.White;
        pnlBoardHost.BorderStyle = BorderStyle.FixedSingle;
        pnlBoardHost.Controls.Add(pnlBoard);
        pnlBoardHost.Dock = DockStyle.Fill;
        pnlBoardHost.Margin = new Padding(0, 0, 20, 0);
        pnlBoardHost.Name = "pnlBoardHost";
        pnlBoardHost.Padding = new Padding(12);
        pnlBoardHost.TabIndex = 0;

        // pnlBoard
        // Bàn cờ sẽ được tạo trong Panel này khi chạy.
        pnlBoard.BackColor = Color.FromArgb(255, 253, 245);
        pnlBoard.Location = new Point(4, 4);
        pnlBoard.Margin = new Padding(0);
        pnlBoard.Name = "pnlBoard";
        pnlBoard.Size = new Size(360, 360);
        pnlBoard.TabIndex = 0;

        // pnlInformation
        pnlInformation.BackColor = Color.White;
        pnlInformation.BorderStyle = BorderStyle.FixedSingle;
        pnlInformation.Controls.Add(tblInformation);
        pnlInformation.Dock = DockStyle.Fill;
        pnlInformation.Margin = new Padding(0);
        pnlInformation.Name = "pnlInformation";
        pnlInformation.Padding = new Padding(16);
        pnlInformation.TabIndex = 1;

        // tblInformation
        tblInformation.ColumnCount = 1;
        tblInformation.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100F));

        tblInformation.RowCount = 9;

        // Tiêu đề.
        tblInformation.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 30F));

        // Người chơi X.
        tblInformation.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 38F));

        // Người chơi O.
        tblInformation.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 38F));

        // Lượt đánh.
        tblInformation.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 26F));

        // Đồng hồ.
        tblInformation.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 42F));

        // Chú thích đồng hồ.
        tblInformation.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 16F));

        // Khoảng trống co giãn.
        tblInformation.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100F));

        // Khán giả.
        tblInformation.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 24F));

        // Các nút thao tác.
        tblInformation.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 42F));

        tblInformation.Controls.Add(lblInformationTitle, 0, 0);
        tblInformation.Controls.Add(lblPlayerX, 0, 1);
        tblInformation.Controls.Add(lblPlayerO, 0, 2);
        tblInformation.Controls.Add(lblTurn, 0, 3);
        tblInformation.Controls.Add(lblTime, 0, 4);
        tblInformation.Controls.Add(lblTimeCaption, 0, 5);
        tblInformation.Controls.Add(lblSpectators, 0, 7);
        tblInformation.Controls.Add(tblActions, 0, 8);

        tblInformation.Dock = DockStyle.Fill;
        tblInformation.Margin = new Padding(0);
        tblInformation.Name = "tblInformation";
        tblInformation.TabIndex = 0;

        // lblInformationTitle
        lblInformationTitle.Dock = DockStyle.Fill;
        lblInformationTitle.Font = new Font(
            "Segoe UI", 13F, FontStyle.Bold);
        lblInformationTitle.ForeColor = Color.FromArgb(23, 43, 77);
        lblInformationTitle.Margin = new Padding(0);
        lblInformationTitle.Name = "lblInformationTitle";
        lblInformationTitle.Text = "THÔNG TIN TRẬN";

        // lblPlayerX
        lblPlayerX.AutoEllipsis = true;
        lblPlayerX.Dock = DockStyle.Fill;
        lblPlayerX.Font = new Font(
            "Segoe UI", 13F, FontStyle.Bold);
        lblPlayerX.ForeColor = Color.FromArgb(37, 99, 235);
        lblPlayerX.BackColor = Color.FromArgb(239, 246, 255);
        lblPlayerX.Margin = new Padding(0, 2, 0, 3);
        lblPlayerX.Padding = new Padding(12, 0, 8, 0);
        lblPlayerX.Name = "lblPlayerX";
        lblPlayerX.Text = "X   Đang chờ...";
        lblPlayerX.TextAlign = ContentAlignment.MiddleLeft;

        // lblPlayerO
        lblPlayerO.AutoEllipsis = true;
        lblPlayerO.Dock = DockStyle.Fill;
        lblPlayerO.Font = new Font(
            "Segoe UI", 13F, FontStyle.Bold);
        lblPlayerO.ForeColor = Color.FromArgb(239, 83, 80);
        lblPlayerO.BackColor = Color.FromArgb(254, 242, 242);
        lblPlayerO.Margin = new Padding(0, 3, 0, 2);
        lblPlayerO.Padding = new Padding(12, 0, 8, 0);
        lblPlayerO.Name = "lblPlayerO";
        lblPlayerO.Text = "O   Đang chờ...";
        lblPlayerO.TextAlign = ContentAlignment.MiddleLeft;

        // lblTurn
        lblTurn.AutoEllipsis = true;
        lblTurn.Dock = DockStyle.Fill;
        lblTurn.Font = new Font(
            "Segoe UI", 11F, FontStyle.Bold);
        lblTurn.ForeColor = Color.FromArgb(37, 99, 235);
        lblTurn.Margin = new Padding(0);
        lblTurn.Name = "lblTurn";
        lblTurn.Text = "Chờ bắt đầu";
        lblTurn.TextAlign = ContentAlignment.BottomLeft;

        // lblTime
        lblTime.Dock = DockStyle.Fill;
        lblTime.Font = new Font(
            "Segoe UI", 26F, FontStyle.Bold);
        lblTime.ForeColor = Color.FromArgb(23, 43, 77);
        lblTime.Margin = new Padding(0);
        lblTime.Name = "lblTime";
        lblTime.Text = "--:--";
        lblTime.BackColor = Color.FromArgb(248, 250, 252);
        lblTime.Padding = new Padding(10, 0, 0, 0);
        lblTime.TextAlign = ContentAlignment.MiddleLeft;

        // lblTimeCaption
        lblTimeCaption.Dock = DockStyle.Fill;
        lblTimeCaption.Font = new Font("Segoe UI", 9F);
        lblTimeCaption.ForeColor = Color.FromArgb(100, 116, 139);
        lblTimeCaption.Margin = new Padding(0);
        lblTimeCaption.Name = "lblTimeCaption";
        lblTimeCaption.Text = "Thời gian còn lại";

        // lblSpectators
        lblSpectators.Dock = DockStyle.Fill;
        lblSpectators.Font = new Font(
            "Segoe UI", 10F, FontStyle.Bold);
        lblSpectators.ForeColor = Color.FromArgb(23, 43, 77);
        lblSpectators.Margin = new Padding(0);
        lblSpectators.Name = "lblSpectators";
        lblSpectators.Text = "Khán giả: 0";
        lblSpectators.TextAlign = ContentAlignment.MiddleLeft;

        // tblActions
        tblActions.ColumnCount = 2;
        tblActions.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 50F));
        tblActions.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 50F));

        tblActions.RowCount = 1;
        tblActions.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100F));

        tblActions.Controls.Add(btnReady, 0, 0);
        tblActions.Controls.Add(btnLeave, 1, 0);

        tblActions.Dock = DockStyle.Fill;
        tblActions.Margin = new Padding(0);
        tblActions.Name = "tblActions";
        tblActions.TabIndex = 0;

        // btnReady
        btnReady.BackColor = Color.FromArgb(37, 99, 235);
        btnReady.Cursor = Cursors.Hand;
        btnReady.Dock = DockStyle.Fill;
        btnReady.Enabled = false;
        btnReady.FlatAppearance.BorderSize = 0;
        btnReady.FlatStyle = FlatStyle.Flat;
        btnReady.Font = new Font(
            "Segoe UI", 9F, FontStyle.Bold);
        btnReady.ForeColor = Color.White;
        btnReady.Margin = new Padding(0, 4, 5, 0);
        btnReady.Name = "btnReady";
        btnReady.TabIndex = 0;
        btnReady.Text = "Sẵn sàng";
        btnReady.UseVisualStyleBackColor = false;

        // btnLeave
        btnLeave.BackColor = Color.White;
        btnLeave.Cursor = Cursors.Hand;
        btnLeave.Dock = DockStyle.Fill;
        btnLeave.Enabled = false;
        btnLeave.FlatAppearance.BorderColor =
            Color.FromArgb(220, 38, 38);
        btnLeave.FlatStyle = FlatStyle.Flat;
        btnLeave.Font = new Font(
            "Segoe UI", 9F, FontStyle.Bold);
        btnLeave.ForeColor = Color.FromArgb(220, 38, 38);
        btnLeave.Margin = new Padding(5, 4, 0, 0);
        btnLeave.Name = "btnLeave";
        btnLeave.TabIndex = 1;
        btnLeave.Text = "Rời phòng";
        btnLeave.UseVisualStyleBackColor = false;

        // lblHint
        lblHint.AutoEllipsis = true;
        lblHint.Dock = DockStyle.Fill;
        lblHint.Font = new Font("Segoe UI", 9F);
        lblHint.ForeColor = Color.FromArgb(100, 116, 139);
        lblHint.Margin = new Padding(2, 4, 0, 0);
        lblHint.Name = "lblHint";
        lblHint.Text = "Tham gia một phòng để bắt đầu chơi.";
        lblHint.TextAlign = ContentAlignment.MiddleLeft;

        // GameView
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(245, 247, 251);
        Controls.Add(tblRoot);
        Font = new Font("Segoe UI", 10F);
        Margin = new Padding(0);
        Name = "GameView";
        Size = new Size(942, 527);

        tblRoot.ResumeLayout(false);
        tblBody.ResumeLayout(false);
        tblInformation.ResumeLayout(false);
        tblActions.ResumeLayout(false);

        pnlHeading.ResumeLayout(false);
        pnlBoardHost.ResumeLayout(false);
        pnlInformation.ResumeLayout(false);

        ResumeLayout(false);
    }
}
