using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Caro.Client.WinForms;

partial class OnlinePlayersView
{
    private IContainer? components = null;

    private TableLayoutPanel tblRoot;
    private TableLayoutPanel tblToolbar;

    private Panel pnlHeading;
    private Label lblTitle;
    private Label lblDescription;
    private Label lblStatus;
    private Button btnRefresh;

    private DataGridView dgvPlayers;

    private DataGridViewTextBoxColumn colNickname;
    private DataGridViewTextBoxColumn colPlayerStatus;
    private DataGridViewTextBoxColumn colWins;
    private DataGridViewTextBoxColumn colLosses;
    private DataGridViewButtonColumn colChallenge;

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
        DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle6 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle4 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle5 = new DataGridViewCellStyle();
        tblRoot = new TableLayoutPanel();
        pnlHeading = new Panel();
        lblTitle = new Label();
        lblDescription = new Label();
        tblToolbar = new TableLayoutPanel();
        btnRefresh = new Button();
        dgvPlayers = new DataGridView();
        colNickname = new DataGridViewTextBoxColumn();
        colPlayerStatus = new DataGridViewTextBoxColumn();
        colWins = new DataGridViewTextBoxColumn();
        colLosses = new DataGridViewTextBoxColumn();
        colChallenge = new DataGridViewButtonColumn();
        lblStatus = new Label();
        tblRoot.SuspendLayout();
        pnlHeading.SuspendLayout();
        tblToolbar.SuspendLayout();
        ((ISupportInitialize)dgvPlayers).BeginInit();
        SuspendLayout();
        // 
        // tblRoot
        // 
        tblRoot.ColumnCount = 1;
        tblRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tblRoot.Controls.Add(pnlHeading, 0, 0);
        tblRoot.Controls.Add(tblToolbar, 0, 1);
        tblRoot.Controls.Add(dgvPlayers, 0, 2);
        tblRoot.Controls.Add(lblStatus, 0, 3);
        tblRoot.Dock = DockStyle.Fill;
        tblRoot.Location = new Point(0, 0);
        tblRoot.Margin = new Padding(0);
        tblRoot.Name = "tblRoot";
        tblRoot.RowCount = 4;
        tblRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 82F));
        tblRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));
        tblRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tblRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        tblRoot.Size = new Size(942, 527);
        tblRoot.TabIndex = 0;
        // 
        // pnlHeading
        // 
        pnlHeading.Controls.Add(lblTitle);
        pnlHeading.Controls.Add(lblDescription);
        pnlHeading.Dock = DockStyle.Fill;
        pnlHeading.Location = new Point(0, 0);
        pnlHeading.Margin = new Padding(0);
        pnlHeading.Name = "pnlHeading";
        pnlHeading.Size = new Size(942, 82);
        pnlHeading.TabIndex = 0;
        // 
        // lblTitle
        // 
        lblTitle.AutoSize = true;
        lblTitle.Font = new Font("Segoe UI", 23F, FontStyle.Bold);
        lblTitle.ForeColor = Color.FromArgb(23, 43, 77);
        lblTitle.Location = new Point(0, 0);
        lblTitle.Name = "lblTitle";
        lblTitle.Size = new Size(354, 52);
        lblTitle.TabIndex = 0;
        lblTitle.Text = "Người chơi online";
        // 
        // lblDescription
        // 
        lblDescription.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        lblDescription.AutoEllipsis = true;
        lblDescription.Font = new Font("Segoe UI", 10F);
        lblDescription.ForeColor = Color.FromArgb(100, 116, 139);
        lblDescription.Location = new Point(3, 49);
        lblDescription.Name = "lblDescription";
        lblDescription.Size = new Size(930, 26);
        lblDescription.TabIndex = 1;
        lblDescription.Text = "Chọn một người chơi để gửi lời mời thách đấu.";
        // 
        // tblToolbar
        // 
        tblToolbar.ColumnCount = 2;
        tblToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tblToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 128F));
        tblToolbar.Controls.Add(btnRefresh, 1, 0);
        tblToolbar.Dock = DockStyle.Fill;
        tblToolbar.Location = new Point(0, 82);
        tblToolbar.Margin = new Padding(0, 0, 0, 12);
        tblToolbar.Name = "tblToolbar";
        tblToolbar.RowCount = 1;
        tblToolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tblToolbar.Size = new Size(942, 48);
        tblToolbar.TabIndex = 0;
        // 
        // btnRefresh
        // 
        btnRefresh.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        btnRefresh.BackColor = Color.White;
        btnRefresh.Cursor = Cursors.Hand;
        btnRefresh.FlatAppearance.BorderColor = Color.FromArgb(37, 99, 235);
        btnRefresh.FlatStyle = FlatStyle.Flat;
        btnRefresh.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnRefresh.ForeColor = Color.FromArgb(37, 99, 235);
        btnRefresh.Location = new Point(814, 6);
        btnRefresh.Margin = new Padding(0);
        btnRefresh.Name = "btnRefresh";
        btnRefresh.Size = new Size(128, 36);
        btnRefresh.TabIndex = 2;
        btnRefresh.Text = "Làm mới";
        btnRefresh.UseVisualStyleBackColor = false;
        // 
        // dgvPlayers
        // 
        dgvPlayers.AllowUserToAddRows = false;
        dgvPlayers.AllowUserToDeleteRows = false;
        dgvPlayers.AllowUserToResizeRows = false;
        dataGridViewCellStyle1.BackColor = Color.FromArgb(250, 252, 255);
        dgvPlayers.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
        dgvPlayers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgvPlayers.BackgroundColor = Color.White;
        dgvPlayers.BorderStyle = BorderStyle.FixedSingle;
        dgvPlayers.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        dgvPlayers.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle2.BackColor = Color.FromArgb(235, 241, 249);
        dataGridViewCellStyle2.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        dataGridViewCellStyle2.ForeColor = Color.FromArgb(23, 43, 77);
        dataGridViewCellStyle2.Padding = new Padding(10, 0, 0, 0);
        dataGridViewCellStyle2.SelectionBackColor = Color.FromArgb(235, 241, 249);
        dataGridViewCellStyle2.SelectionForeColor = Color.FromArgb(23, 43, 77);
        dataGridViewCellStyle2.WrapMode = DataGridViewTriState.True;
        dgvPlayers.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
        dgvPlayers.ColumnHeadersHeight = 44;
        dgvPlayers.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        dgvPlayers.Columns.AddRange(new DataGridViewColumn[] { colNickname, colPlayerStatus, colWins, colLosses, colChallenge });
        dataGridViewCellStyle6.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle6.BackColor = Color.White;
        dataGridViewCellStyle6.Font = new Font("Segoe UI", 10F);
        dataGridViewCellStyle6.ForeColor = Color.FromArgb(23, 43, 77);
        dataGridViewCellStyle6.Padding = new Padding(10, 0, 0, 0);
        dataGridViewCellStyle6.SelectionBackColor = Color.FromArgb(225, 237, 255);
        dataGridViewCellStyle6.SelectionForeColor = Color.FromArgb(23, 43, 77);
        dataGridViewCellStyle6.WrapMode = DataGridViewTriState.False;
        dgvPlayers.DefaultCellStyle = dataGridViewCellStyle6;
        dgvPlayers.Dock = DockStyle.Fill;
        dgvPlayers.EnableHeadersVisualStyles = false;
        dgvPlayers.GridColor = Color.FromArgb(220, 227, 237);
        dgvPlayers.Location = new Point(0, 142);
        dgvPlayers.Margin = new Padding(0);
        dgvPlayers.MultiSelect = false;
        dgvPlayers.Name = "dgvPlayers";
        dgvPlayers.ReadOnly = true;
        dgvPlayers.RowHeadersVisible = false;
        dgvPlayers.RowHeadersWidth = 51;
        dgvPlayers.RowTemplate.Height = 48;
        dgvPlayers.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvPlayers.Size = new Size(942, 347);
        dgvPlayers.TabIndex = 1;
        // 
        // colNickname
        // 
        colNickname.FillWeight = 30F;
        colNickname.HeaderText = "Người chơi";
        colNickname.MinimumWidth = 150;
        colNickname.Name = "colNickname";
        colNickname.ReadOnly = true;
        // 
        // colPlayerStatus
        // 
        colPlayerStatus.FillWeight = 24F;
        colPlayerStatus.HeaderText = "Trạng thái";
        colPlayerStatus.MinimumWidth = 120;
        colPlayerStatus.Name = "colPlayerStatus";
        colPlayerStatus.ReadOnly = true;
        // 
        // colWins
        // 
        dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleCenter;
        colWins.DefaultCellStyle = dataGridViewCellStyle3;
        colWins.FillWeight = 12F;
        colWins.HeaderText = "Thắng";
        colWins.MinimumWidth = 60;
        colWins.Name = "colWins";
        colWins.ReadOnly = true;
        // 
        // colLosses
        // 
        dataGridViewCellStyle4.Alignment = DataGridViewContentAlignment.MiddleCenter;
        colLosses.DefaultCellStyle = dataGridViewCellStyle4;
        colLosses.FillWeight = 12F;
        colLosses.HeaderText = "Thua";
        colLosses.MinimumWidth = 60;
        colLosses.Name = "colLosses";
        colLosses.ReadOnly = true;
        // 
        // colChallenge
        // 
        dataGridViewCellStyle5.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dataGridViewCellStyle5.Padding = new Padding(8, 6, 8, 6);
        colChallenge.DefaultCellStyle = dataGridViewCellStyle5;
        colChallenge.FillWeight = 22F;
        colChallenge.FlatStyle = FlatStyle.Flat;
        colChallenge.HeaderText = "Thách đấu";
        colChallenge.MinimumWidth = 130;
        colChallenge.Name = "colChallenge";
        colChallenge.ReadOnly = true;
        // 
        // lblStatus
        // 
        lblStatus.Dock = DockStyle.Fill;
        lblStatus.Font = new Font("Segoe UI", 9F);
        lblStatus.ForeColor = Color.FromArgb(100, 116, 139);
        lblStatus.Location = new Point(0, 489);
        lblStatus.Margin = new Padding(0);
        lblStatus.Name = "lblStatus";
        lblStatus.Size = new Size(942, 38);
        lblStatus.TabIndex = 2;
        lblStatus.Text = "Chưa tải danh sách người chơi.";
        lblStatus.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // OnlinePlayersView
        // 
        AutoScaleDimensions = new SizeF(120F, 120F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(245, 247, 251);
        Controls.Add(tblRoot);
        Font = new Font("Segoe UI", 10F);
        Margin = new Padding(0);
        Name = "OnlinePlayersView";
        Size = new Size(942, 527);
        tblRoot.ResumeLayout(false);
        pnlHeading.ResumeLayout(false);
        pnlHeading.PerformLayout();
        tblToolbar.ResumeLayout(false);
        ((ISupportInitialize)dgvPlayers).EndInit();
        ResumeLayout(false);
    }
}
