using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Caro.Client.WinForms;

partial class MatchesView
{
    private IContainer? components = null;

    private TableLayoutPanel tblRoot;
    private TableLayoutPanel tblToolbar;

    private Panel pnlHeading;

    private Label lblTitle;
    private Label lblDescription;
    private Label lblStatus;
    private Button btnRefresh;

    private DataGridView dgvMatches;

    private DataGridViewTextBoxColumn colPlayerX;
    private DataGridViewTextBoxColumn colPlayerO;
    private DataGridViewTextBoxColumn colSpectators;
    private DataGridViewTextBoxColumn colMatchStatus;
    private DataGridViewButtonColumn colWatch;

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
        DataGridViewCellStyle dataGridViewCellStyle8 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle4 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle5 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle6 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle7 = new DataGridViewCellStyle();
        tblRoot = new TableLayoutPanel();
        pnlHeading = new Panel();
        lblTitle = new Label();
        lblDescription = new Label();
        tblToolbar = new TableLayoutPanel();
        btnRefresh = new Button();
        dgvMatches = new DataGridView();
        colPlayerX = new DataGridViewTextBoxColumn();
        colPlayerO = new DataGridViewTextBoxColumn();
        colSpectators = new DataGridViewTextBoxColumn();
        colMatchStatus = new DataGridViewTextBoxColumn();
        colWatch = new DataGridViewButtonColumn();
        lblStatus = new Label();
        tblRoot.SuspendLayout();
        pnlHeading.SuspendLayout();
        tblToolbar.SuspendLayout();
        ((ISupportInitialize)dgvMatches).BeginInit();
        SuspendLayout();
        // 
        // tblRoot
        // 
        tblRoot.ColumnCount = 1;
        tblRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tblRoot.Controls.Add(pnlHeading, 0, 0);
        tblRoot.Controls.Add(tblToolbar, 0, 1);
        tblRoot.Controls.Add(dgvMatches, 0, 2);
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
        lblTitle.Size = new Size(297, 52);
        lblTitle.TabIndex = 0;
        lblTitle.Text = "Trận đang chơi";
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
        lblDescription.Text = "Theo dõi các trận đấu đang diễn ra trong mạng LAN.";
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
        btnRefresh.TabIndex = 1;
        btnRefresh.Text = "Làm mới";
        btnRefresh.UseVisualStyleBackColor = false;
        // 
        // dgvMatches
        // 
        dgvMatches.AllowUserToAddRows = false;
        dgvMatches.AllowUserToDeleteRows = false;
        dgvMatches.AllowUserToResizeRows = false;
        dataGridViewCellStyle1.BackColor = Color.FromArgb(250, 252, 255);
        dgvMatches.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
        dgvMatches.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgvMatches.BackgroundColor = Color.White;
        dgvMatches.BorderStyle = BorderStyle.FixedSingle;
        dgvMatches.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        dgvMatches.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle2.BackColor = Color.FromArgb(235, 241, 249);
        dataGridViewCellStyle2.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        dataGridViewCellStyle2.ForeColor = Color.FromArgb(23, 43, 77);
        dataGridViewCellStyle2.Padding = new Padding(8, 0, 0, 0);
        dataGridViewCellStyle2.SelectionBackColor = Color.FromArgb(235, 241, 249);
        dataGridViewCellStyle2.SelectionForeColor = Color.FromArgb(23, 43, 77);
        dataGridViewCellStyle2.WrapMode = DataGridViewTriState.True;
        dgvMatches.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
        dgvMatches.ColumnHeadersHeight = 44;
        dgvMatches.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        dgvMatches.Columns.AddRange(new DataGridViewColumn[] { colPlayerX, colPlayerO, colSpectators, colMatchStatus, colWatch });
        dataGridViewCellStyle8.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle8.BackColor = Color.White;
        dataGridViewCellStyle8.Font = new Font("Segoe UI", 10F);
        dataGridViewCellStyle8.ForeColor = Color.FromArgb(23, 43, 77);
        dataGridViewCellStyle8.Padding = new Padding(8, 0, 0, 0);
        dataGridViewCellStyle8.SelectionBackColor = Color.FromArgb(225, 237, 255);
        dataGridViewCellStyle8.SelectionForeColor = Color.FromArgb(23, 43, 77);
        dataGridViewCellStyle8.WrapMode = DataGridViewTriState.False;
        dgvMatches.DefaultCellStyle = dataGridViewCellStyle8;
        dgvMatches.Dock = DockStyle.Fill;
        dgvMatches.EnableHeadersVisualStyles = false;
        dgvMatches.GridColor = Color.FromArgb(220, 227, 237);
        dgvMatches.Location = new Point(0, 142);
        dgvMatches.Margin = new Padding(0);
        dgvMatches.MultiSelect = false;
        dgvMatches.Name = "dgvMatches";
        dgvMatches.ReadOnly = true;
        dgvMatches.RowHeadersVisible = false;
        dgvMatches.RowHeadersWidth = 51;
        dgvMatches.RowTemplate.Height = 50;
        dgvMatches.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvMatches.Size = new Size(942, 347);
        dgvMatches.TabIndex = 1;
        // 
        // colPlayerX
        // 
        dataGridViewCellStyle3.ForeColor = Color.FromArgb(37, 99, 235);
        colPlayerX.DefaultCellStyle = dataGridViewCellStyle3;
        colPlayerX.FillWeight = 24F;
        colPlayerX.HeaderText = "Người chơi X";
        colPlayerX.MinimumWidth = 110;
        colPlayerX.Name = "colPlayerX";
        colPlayerX.ReadOnly = true;
        // 
        // colPlayerO
        // 
        dataGridViewCellStyle4.ForeColor = Color.FromArgb(239, 83, 80);
        colPlayerO.DefaultCellStyle = dataGridViewCellStyle4;
        colPlayerO.FillWeight = 24F;
        colPlayerO.HeaderText = "Người chơi O";
        colPlayerO.MinimumWidth = 110;
        colPlayerO.Name = "colPlayerO";
        colPlayerO.ReadOnly = true;
        // 
        // colSpectators
        // 
        dataGridViewCellStyle5.Alignment = DataGridViewContentAlignment.MiddleCenter;
        colSpectators.DefaultCellStyle = dataGridViewCellStyle5;
        colSpectators.FillWeight = 12F;
        colSpectators.HeaderText = "Khán giả";
        colSpectators.MinimumWidth = 80;
        colSpectators.Name = "colSpectators";
        colSpectators.ReadOnly = true;
        // 
        // colMatchStatus
        // 
        dataGridViewCellStyle6.ForeColor = Color.FromArgb(22, 163, 74);
        colMatchStatus.DefaultCellStyle = dataGridViewCellStyle6;
        colMatchStatus.FillWeight = 18F;
        colMatchStatus.HeaderText = "Trạng thái";
        colMatchStatus.MinimumWidth = 110;
        colMatchStatus.Name = "colMatchStatus";
        colMatchStatus.ReadOnly = true;
        // 
        // colWatch
        // 
        dataGridViewCellStyle7.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dataGridViewCellStyle7.BackColor = Color.FromArgb(37, 99, 235);
        dataGridViewCellStyle7.ForeColor = Color.White;
        dataGridViewCellStyle7.Padding = new Padding(6, 7, 6, 7);
        dataGridViewCellStyle7.SelectionBackColor = Color.FromArgb(29, 78, 216);
        dataGridViewCellStyle7.SelectionForeColor = Color.White;
        colWatch.DefaultCellStyle = dataGridViewCellStyle7;
        colWatch.FillWeight = 17F;
        colWatch.FlatStyle = FlatStyle.Flat;
        colWatch.HeaderText = "Thao tác";
        colWatch.MinimumWidth = 100;
        colWatch.Name = "colWatch";
        colWatch.ReadOnly = true;
        colWatch.Text = "Xem trận";
        colWatch.UseColumnTextForButtonValue = true;
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
        lblStatus.Text = "Chưa tải danh sách trận đấu.";
        lblStatus.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // MatchesView
        // 
        AutoScaleDimensions = new SizeF(120F, 120F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(245, 247, 251);
        Controls.Add(tblRoot);
        Font = new Font("Segoe UI", 10F);
        Margin = new Padding(0);
        Name = "MatchesView";
        Size = new Size(942, 527);
        tblRoot.ResumeLayout(false);
        pnlHeading.ResumeLayout(false);
        pnlHeading.PerformLayout();
        tblToolbar.ResumeLayout(false);
        ((ISupportInitialize)dgvMatches).EndInit();
        ResumeLayout(false);
    }
}
