using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Caro.Client.WinForms;

partial class InvitationsView
{
    private IContainer? components = null;

    private TableLayoutPanel tblRoot;
    private TableLayoutPanel tblToolbar;

    private Panel pnlHeading;

    private Label lblTitle;
    private Label lblDescription;
    private Label lblHint;
    private Label lblStatus;

    private Button btnRefresh;

    private TabControl tabInvitations;
    private TabPage tabReceived;
    private TabPage tabSent;

    private DataGridView dgvReceived;
    private DataGridView dgvSent;

    private DataGridViewTextBoxColumn colSender;
    private DataGridViewTextBoxColumn colReceivedTime;
    private DataGridViewTextBoxColumn colReceivedStatus;
    private DataGridViewButtonColumn colAccept;
    private DataGridViewButtonColumn colDecline;

    private DataGridViewTextBoxColumn colRecipient;
    private DataGridViewTextBoxColumn colSentTime;
    private DataGridViewTextBoxColumn colSentStatus;
    private DataGridViewButtonColumn colCancel;

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
        tblToolbar = new TableLayoutPanel();

        pnlHeading = new Panel();

        lblTitle = new Label();
        lblDescription = new Label();
        lblHint = new Label();
        lblStatus = new Label();

        btnRefresh = new Button();

        tabInvitations = new TabControl();
        tabReceived = new TabPage();
        tabSent = new TabPage();

        dgvReceived = new DataGridView();
        dgvSent = new DataGridView();

        colSender = new DataGridViewTextBoxColumn();
        colReceivedTime = new DataGridViewTextBoxColumn();
        colReceivedStatus = new DataGridViewTextBoxColumn();
        colAccept = new DataGridViewButtonColumn();
        colDecline = new DataGridViewButtonColumn();

        colRecipient = new DataGridViewTextBoxColumn();
        colSentTime = new DataGridViewTextBoxColumn();
        colSentStatus = new DataGridViewTextBoxColumn();
        colCancel = new DataGridViewButtonColumn();

        tblRoot.SuspendLayout();
        tblToolbar.SuspendLayout();
        pnlHeading.SuspendLayout();

        tabInvitations.SuspendLayout();
        tabReceived.SuspendLayout();
        tabSent.SuspendLayout();

        ((ISupportInitialize)dgvReceived).BeginInit();
        ((ISupportInitialize)dgvSent).BeginInit();

        SuspendLayout();

        // tblRoot
        tblRoot.ColumnCount = 1;
        tblRoot.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100F));

        tblRoot.RowCount = 4;
        tblRoot.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 82F));
        tblRoot.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 52F));
        tblRoot.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100F));
        tblRoot.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 38F));

        tblRoot.Controls.Add(pnlHeading, 0, 0);
        tblRoot.Controls.Add(tblToolbar, 0, 1);
        tblRoot.Controls.Add(tabInvitations, 0, 2);
        tblRoot.Controls.Add(lblStatus, 0, 3);

        tblRoot.Dock = DockStyle.Fill;
        tblRoot.Margin = new Padding(0);
        tblRoot.Name = "tblRoot";
        tblRoot.Size = new Size(942, 527);
        tblRoot.TabIndex = 0;

        // pnlHeading
        pnlHeading.Controls.Add(lblTitle);
        pnlHeading.Controls.Add(lblDescription);
        pnlHeading.Dock = DockStyle.Fill;
        pnlHeading.Margin = new Padding(0);
        pnlHeading.Name = "pnlHeading";
        pnlHeading.Size = new Size(942, 82);

        // lblTitle
        lblTitle.AutoSize = true;
        lblTitle.Font = new Font(
            "Segoe UI", 23F, FontStyle.Bold);
        lblTitle.ForeColor = Color.FromArgb(23, 43, 77);
        lblTitle.Location = new Point(0, 0);
        lblTitle.Name = "lblTitle";
        lblTitle.Text = "Lời mời thách đấu";

        // lblDescription
        lblDescription.Anchor =
            AnchorStyles.Top |
            AnchorStyles.Left |
            AnchorStyles.Right;

        lblDescription.AutoEllipsis = true;
        lblDescription.Font = new Font("Segoe UI", 10F);
        lblDescription.ForeColor = Color.FromArgb(100, 116, 139);
        lblDescription.Location = new Point(3, 49);
        lblDescription.Name = "lblDescription";
        lblDescription.Size = new Size(930, 26);
        lblDescription.Text =
            "Quản lý lời mời nhận được và lời mời đã gửi.";

        // tblToolbar
        tblToolbar.ColumnCount = 2;
        tblToolbar.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100F));
        tblToolbar.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 120F));

        tblToolbar.RowCount = 1;
        tblToolbar.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100F));

        tblToolbar.Controls.Add(lblHint, 0, 0);
        tblToolbar.Controls.Add(btnRefresh, 1, 0);

        tblToolbar.Dock = DockStyle.Fill;
        tblToolbar.Margin = new Padding(0, 0, 0, 12);
        tblToolbar.Name = "tblToolbar";
        tblToolbar.TabIndex = 0;

        // lblHint
        lblHint.AutoEllipsis = true;
        lblHint.Dock = DockStyle.Fill;
        lblHint.ForeColor = Color.FromArgb(100, 116, 139);
        lblHint.Margin = new Padding(0, 0, 16, 0);
        lblHint.Name = "lblHint";
        lblHint.Text = "Chấp nhận lời mời để vào phòng chờ.";
        lblHint.TextAlign = ContentAlignment.MiddleLeft;

        // btnRefresh
        btnRefresh.BackColor = Color.White;
        btnRefresh.Cursor = Cursors.Hand;
        btnRefresh.Dock = DockStyle.Fill;
        btnRefresh.FlatAppearance.BorderColor =
            Color.FromArgb(37, 99, 235);
        btnRefresh.FlatStyle = FlatStyle.Flat;
        btnRefresh.Font = new Font(
            "Segoe UI", 10F, FontStyle.Bold);
        btnRefresh.ForeColor = Color.FromArgb(37, 99, 235);
        btnRefresh.Margin = new Padding(0);
        btnRefresh.Name = "btnRefresh";
        btnRefresh.TabIndex = 0;
        btnRefresh.Text = "Làm mới";
        btnRefresh.UseVisualStyleBackColor = false;

        // tabInvitations
        tabInvitations.Controls.Add(tabReceived);
        tabInvitations.Controls.Add(tabSent);
        tabInvitations.Dock = DockStyle.Fill;
        tabInvitations.Font = new Font("Segoe UI", 10F);
        tabInvitations.Margin = new Padding(0);
        tabInvitations.Name = "tabInvitations";
        tabInvitations.Padding = new Point(18, 8);
        tabInvitations.SelectedIndex = 0;
        tabInvitations.TabIndex = 1;

        // tabReceived
        tabReceived.BackColor = Color.White;
        tabReceived.Controls.Add(dgvReceived);
        tabReceived.Name = "tabReceived";
        tabReceived.Padding = new Padding(8);
        tabReceived.TabIndex = 0;
        tabReceived.Text = "Đã nhận";

        // tabSent
        tabSent.BackColor = Color.White;
        tabSent.Controls.Add(dgvSent);
        tabSent.Name = "tabSent";
        tabSent.Padding = new Padding(8);
        tabSent.TabIndex = 1;
        tabSent.Text = "Đã gửi";

        // dgvReceived
        dgvReceived.AllowUserToAddRows = false;
        dgvReceived.AllowUserToDeleteRows = false;
        dgvReceived.AllowUserToResizeRows = false;
        dgvReceived.AutoGenerateColumns = false;
        dgvReceived.AutoSizeColumnsMode =
            DataGridViewAutoSizeColumnsMode.Fill;

        dgvReceived.BackgroundColor = Color.White;
        dgvReceived.BorderStyle = BorderStyle.None;
        dgvReceived.CellBorderStyle =
            DataGridViewCellBorderStyle.SingleHorizontal;

        dgvReceived.ColumnHeadersBorderStyle =
            DataGridViewHeaderBorderStyle.Single;
        dgvReceived.ColumnHeadersHeight = 44;
        dgvReceived.ColumnHeadersHeightSizeMode =
            DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

        dgvReceived.ColumnHeadersDefaultCellStyle.BackColor =
            Color.FromArgb(235, 241, 249);
        dgvReceived.ColumnHeadersDefaultCellStyle.ForeColor =
            Color.FromArgb(23, 43, 77);
        dgvReceived.ColumnHeadersDefaultCellStyle.Font =
            new Font("Segoe UI", 10F, FontStyle.Bold);
        dgvReceived.ColumnHeadersDefaultCellStyle.Alignment =
            DataGridViewContentAlignment.MiddleLeft;
        dgvReceived.ColumnHeadersDefaultCellStyle.Padding =
            new Padding(8, 0, 0, 0);
        dgvReceived.ColumnHeadersDefaultCellStyle.SelectionBackColor =
            Color.FromArgb(235, 241, 249);
        dgvReceived.ColumnHeadersDefaultCellStyle.SelectionForeColor =
            Color.FromArgb(23, 43, 77);

        dgvReceived.DefaultCellStyle.BackColor = Color.White;
        dgvReceived.DefaultCellStyle.ForeColor =
            Color.FromArgb(23, 43, 77);
        dgvReceived.DefaultCellStyle.Font =
            new Font("Segoe UI", 10F);
        dgvReceived.DefaultCellStyle.Padding =
            new Padding(8, 0, 0, 0);
        dgvReceived.DefaultCellStyle.SelectionBackColor =
            Color.FromArgb(225, 237, 255);
        dgvReceived.DefaultCellStyle.SelectionForeColor =
            Color.FromArgb(23, 43, 77);

        dgvReceived.AlternatingRowsDefaultCellStyle.BackColor =
            Color.FromArgb(250, 252, 255);

        dgvReceived.Columns.AddRange(new DataGridViewColumn[]
        {
            colSender,
            colReceivedTime,
            colReceivedStatus,
            colAccept,
            colDecline
        });

        dgvReceived.Dock = DockStyle.Fill;
        dgvReceived.EnableHeadersVisualStyles = false;
        dgvReceived.GridColor = Color.FromArgb(220, 227, 237);
        dgvReceived.Margin = new Padding(0);
        dgvReceived.MultiSelect = false;
        dgvReceived.Name = "dgvReceived";
        dgvReceived.ReadOnly = true;
        dgvReceived.RowHeadersVisible = false;
        dgvReceived.RowTemplate.Height = 50;
        dgvReceived.SelectionMode =
            DataGridViewSelectionMode.FullRowSelect;
        dgvReceived.TabIndex = 0;

        // colSender
        colSender.FillWeight = 25F;
        colSender.HeaderText = "Người mời";
        colSender.MinimumWidth = 120;
        colSender.Name = "colSender";
        colSender.ReadOnly = true;

        // colReceivedTime
        colReceivedTime.FillWeight = 22F;
        colReceivedTime.HeaderText = "Thời gian còn lại";
        colReceivedTime.MinimumWidth = 140;
        colReceivedTime.Name = "colReceivedTime";
        colReceivedTime.ReadOnly = true;
        colReceivedTime.SortMode =
            DataGridViewColumnSortMode.NotSortable;

        // colReceivedStatus
        colReceivedStatus.FillWeight = 21F;
        colReceivedStatus.HeaderText = "Trạng thái";
        colReceivedStatus.MinimumWidth = 110;
        colReceivedStatus.Name = "colReceivedStatus";
        colReceivedStatus.ReadOnly = true;

        // colAccept
        colAccept.DefaultCellStyle.Alignment =
            DataGridViewContentAlignment.MiddleCenter;
        colAccept.DefaultCellStyle.Padding =
            new Padding(6, 7, 6, 7);
        colAccept.DefaultCellStyle.BackColor =
            Color.FromArgb(37, 99, 235);
        colAccept.DefaultCellStyle.ForeColor = Color.White;
        colAccept.DefaultCellStyle.SelectionBackColor =
            Color.FromArgb(29, 78, 216);
        colAccept.DefaultCellStyle.SelectionForeColor = Color.White;
        colAccept.FillWeight = 17F;
        colAccept.FlatStyle = FlatStyle.Flat;
        colAccept.HeaderText = "Chấp nhận";
        colAccept.MinimumWidth = 110;
        colAccept.Name = "colAccept";
        colAccept.ReadOnly = true;
        colAccept.Text = "Chấp nhận";
        colAccept.UseColumnTextForButtonValue = true;

        // colDecline
        colDecline.DefaultCellStyle.Alignment =
            DataGridViewContentAlignment.MiddleCenter;
        colDecline.DefaultCellStyle.Padding =
            new Padding(6, 7, 6, 7);
        colDecline.FillWeight = 15F;
        colDecline.FlatStyle = FlatStyle.Flat;
        colDecline.HeaderText = "Từ chối";
        colDecline.MinimumWidth = 90;
        colDecline.Name = "colDecline";
        colDecline.ReadOnly = true;
        colDecline.Text = "Từ chối";
        colDecline.UseColumnTextForButtonValue = true;

        // dgvSent
        dgvSent.AllowUserToAddRows = false;
        dgvSent.AllowUserToDeleteRows = false;
        dgvSent.AllowUserToResizeRows = false;
        dgvSent.AutoGenerateColumns = false;
        dgvSent.AutoSizeColumnsMode =
            DataGridViewAutoSizeColumnsMode.Fill;

        dgvSent.BackgroundColor = Color.White;
        dgvSent.BorderStyle = BorderStyle.None;
        dgvSent.CellBorderStyle =
            DataGridViewCellBorderStyle.SingleHorizontal;

        dgvSent.ColumnHeadersBorderStyle =
            DataGridViewHeaderBorderStyle.Single;
        dgvSent.ColumnHeadersHeight = 44;
        dgvSent.ColumnHeadersHeightSizeMode =
            DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

        dgvSent.ColumnHeadersDefaultCellStyle.BackColor =
            Color.FromArgb(235, 241, 249);
        dgvSent.ColumnHeadersDefaultCellStyle.ForeColor =
            Color.FromArgb(23, 43, 77);
        dgvSent.ColumnHeadersDefaultCellStyle.Font =
            new Font("Segoe UI", 10F, FontStyle.Bold);
        dgvSent.ColumnHeadersDefaultCellStyle.Alignment =
            DataGridViewContentAlignment.MiddleLeft;
        dgvSent.ColumnHeadersDefaultCellStyle.Padding =
            new Padding(8, 0, 0, 0);
        dgvSent.ColumnHeadersDefaultCellStyle.SelectionBackColor =
            Color.FromArgb(235, 241, 249);
        dgvSent.ColumnHeadersDefaultCellStyle.SelectionForeColor =
            Color.FromArgb(23, 43, 77);

        dgvSent.DefaultCellStyle.BackColor = Color.White;
        dgvSent.DefaultCellStyle.ForeColor =
            Color.FromArgb(23, 43, 77);
        dgvSent.DefaultCellStyle.Font =
            new Font("Segoe UI", 10F);
        dgvSent.DefaultCellStyle.Padding =
            new Padding(8, 0, 0, 0);
        dgvSent.DefaultCellStyle.SelectionBackColor =
            Color.FromArgb(225, 237, 255);
        dgvSent.DefaultCellStyle.SelectionForeColor =
            Color.FromArgb(23, 43, 77);

        dgvSent.AlternatingRowsDefaultCellStyle.BackColor =
            Color.FromArgb(250, 252, 255);

        dgvSent.Columns.AddRange(new DataGridViewColumn[]
        {
            colRecipient,
            colSentTime,
            colSentStatus,
            colCancel
        });

        dgvSent.Dock = DockStyle.Fill;
        dgvSent.EnableHeadersVisualStyles = false;
        dgvSent.GridColor = Color.FromArgb(220, 227, 237);
        dgvSent.Margin = new Padding(0);
        dgvSent.MultiSelect = false;
        dgvSent.Name = "dgvSent";
        dgvSent.ReadOnly = true;
        dgvSent.RowHeadersVisible = false;
        dgvSent.RowTemplate.Height = 50;
        dgvSent.SelectionMode =
            DataGridViewSelectionMode.FullRowSelect;
        dgvSent.TabIndex = 0;

        // colRecipient
        colRecipient.FillWeight = 30F;
        colRecipient.HeaderText = "Người nhận";
        colRecipient.MinimumWidth = 140;
        colRecipient.Name = "colRecipient";
        colRecipient.ReadOnly = true;

        // colSentTime
        colSentTime.FillWeight = 25F;
        colSentTime.HeaderText = "Thời gian còn lại";
        colSentTime.MinimumWidth = 140;
        colSentTime.Name = "colSentTime";
        colSentTime.ReadOnly = true;
        colSentTime.SortMode =
            DataGridViewColumnSortMode.NotSortable;

        // colSentStatus
        colSentStatus.FillWeight = 25F;
        colSentStatus.HeaderText = "Trạng thái";
        colSentStatus.MinimumWidth = 120;
        colSentStatus.Name = "colSentStatus";
        colSentStatus.ReadOnly = true;

        // colCancel
        colCancel.DefaultCellStyle.Alignment =
            DataGridViewContentAlignment.MiddleCenter;
        colCancel.DefaultCellStyle.Padding =
            new Padding(6, 7, 6, 7);
        colCancel.DefaultCellStyle.ForeColor =
            Color.FromArgb(220, 38, 38);
        colCancel.DefaultCellStyle.SelectionForeColor =
            Color.FromArgb(220, 38, 38);
        colCancel.FillWeight = 20F;
        colCancel.FlatStyle = FlatStyle.Flat;
        colCancel.HeaderText = "Thao tác";
        colCancel.MinimumWidth = 130;
        colCancel.Name = "colCancel";
        colCancel.ReadOnly = true;
        colCancel.Text = "Hủy lời mời";
        colCancel.UseColumnTextForButtonValue = true;

        // lblStatus
        lblStatus.Dock = DockStyle.Fill;
        lblStatus.ForeColor = Color.FromArgb(100, 116, 139);
        lblStatus.Margin = new Padding(0);
        lblStatus.Name = "lblStatus";
        lblStatus.Text =
            "Lời mời hết hạn sẽ không thể chấp nhận.";
        lblStatus.TextAlign = ContentAlignment.MiddleLeft;

        // InvitationsView
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(245, 247, 251);
        Controls.Add(tblRoot);
        Font = new Font("Segoe UI", 10F);
        Margin = new Padding(0);
        Name = "InvitationsView";
        Size = new Size(942, 527);

        tblRoot.ResumeLayout(false);
        tblToolbar.ResumeLayout(false);

        pnlHeading.ResumeLayout(false);
        pnlHeading.PerformLayout();

        tabInvitations.ResumeLayout(false);
        tabReceived.ResumeLayout(false);
        tabSent.ResumeLayout(false);

        ((ISupportInitialize)dgvReceived).EndInit();
        ((ISupportInitialize)dgvSent).EndInit();

        ResumeLayout(false);
    }
}