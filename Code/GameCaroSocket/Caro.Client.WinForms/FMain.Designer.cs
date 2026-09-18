using System.ComponentModel;

namespace Caro.Client.WinForms
{
    partial class FMain
    {
        private IContainer? components = null;

        private TableLayoutPanel tblRoot;
        private TableLayoutPanel tblBody;

        private Panel pnlHeader;
        private Panel pnlSidebar;
        private Panel pnlContent;

        private FlowLayoutPanel flpNavigation;
        private Label lblAppName;
        private Label lblDescription;
        private Label lblConnection;
        private Label lblNickname;
        private Label lblNavigation;

        private Button btnHome;
        private Button btnOnlinePlayers;
        private Button btnInvitations;
        private Button btnMatches;
        private Button btnGame;
        private Button btnConnect;
        private Button btnDisconnect;

        private StatusStrip statusMain;
        private ToolStripStatusLabel lblServer;
        private ToolStripStatusLabel lblStatusSpacer;



        private void InitializeComponent()
        {
            ComponentResourceManager resources = new ComponentResourceManager(typeof(FMain));
            tblRoot = new TableLayoutPanel();
            pnlHeader = new Panel();
            pictureBox1 = new PictureBox();
            lblAppName = new Label();
            lblDescription = new Label();
            lblConnection = new Label();
            lblNickname = new Label();
            tblBody = new TableLayoutPanel();
            pnlSidebar = new Panel();
            flpNavigation = new FlowLayoutPanel();
            btnHome = new Button();
            btnOnlinePlayers = new Button();
            btnInvitations = new Button();
            btnMatches = new Button();
            btnGame = new Button();
            btnConnect = new Button();
            btnDisconnect = new Button();
            lblNavigation = new Label();
            pnlContent = new Panel();
            statusMain = new StatusStrip();
            lblServer = new ToolStripStatusLabel();
            lblStatusSpacer = new ToolStripStatusLabel();
            tblRoot.SuspendLayout();
            pnlHeader.SuspendLayout();
            ((ISupportInitialize)pictureBox1).BeginInit();
            tblBody.SuspendLayout();
            pnlSidebar.SuspendLayout();
            flpNavigation.SuspendLayout();
            statusMain.SuspendLayout();
            SuspendLayout();
            // 
            // tblRoot
            // 
            tblRoot.ColumnCount = 1;
            tblRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblRoot.Controls.Add(pnlHeader, 0, 0);
            tblRoot.Controls.Add(tblBody, 0, 1);
            tblRoot.Controls.Add(statusMain, 0, 2);
            tblRoot.Dock = DockStyle.Fill;
            tblRoot.Location = new Point(0, 0);
            tblRoot.Margin = new Padding(0);
            tblRoot.Name = "tblRoot";
            tblRoot.RowCount = 3;
            tblRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 76F));
            tblRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tblRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            tblRoot.Size = new Size(1200, 675);
            tblRoot.TabIndex = 0;
            // 
            // pnlHeader
            // 
            pnlHeader.BackColor = Color.White;
            pnlHeader.Controls.Add(pictureBox1);
            pnlHeader.Controls.Add(lblAppName);
            pnlHeader.Controls.Add(lblDescription);
            pnlHeader.Controls.Add(lblConnection);
            pnlHeader.Controls.Add(lblNickname);
            pnlHeader.Dock = DockStyle.Fill;
            pnlHeader.Location = new Point(0, 0);
            pnlHeader.Margin = new Padding(0);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Size = new Size(1200, 72);
            pnlHeader.TabIndex = 0;
            // 
            // pictureBox1
            // 
            pictureBox1.BorderStyle = BorderStyle.None;
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new Point(16, 8);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(56, 60);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 5;
            pictureBox1.TabStop = false;
            // 
            // lblAppName
            // 
            lblAppName.AutoSize = true;
            lblAppName.Font = new Font("Segoe UI", 19F, FontStyle.Bold);
            lblAppName.ForeColor = Color.FromArgb(23, 43, 77);
            lblAppName.Location = new Point(82, 16);
            lblAppName.Name = "lblAppName";
            lblAppName.Size = new Size(182, 45);
            lblAppName.TabIndex = 1;
            lblAppName.Text = "CARO LAN";
            // 
            // lblDescription
            // 
            lblDescription.AutoSize = true;
            lblDescription.Font = new Font("Segoe UI", 10F);
            lblDescription.ForeColor = Color.FromArgb(100, 116, 139);
            lblDescription.Location = new Point(272, 29);
            lblDescription.Name = "lblDescription";
            lblDescription.Size = new Size(279, 23);
            lblDescription.TabIndex = 2;
            lblDescription.Text = "Chơi cùng bạn bè trong mạng LAN";
            // 
            // lblConnection
            // 
            lblConnection.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblConnection.BackColor = Color.FromArgb(241, 245, 249);
            lblConnection.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblConnection.ForeColor = Color.FromArgb(100, 116, 139);
            lblConnection.Location = new Point(878, 20);
            lblConnection.Name = "lblConnection";
            lblConnection.Size = new Size(176, 34);
            lblConnection.TabIndex = 3;
            lblConnection.Text = "● Chưa kết nối";
            lblConnection.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblNickname
            // 
            lblNickname.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblNickname.AutoEllipsis = true;
            lblNickname.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblNickname.ForeColor = Color.FromArgb(23, 43, 77);
            lblNickname.Location = new Point(1066, 22);
            lblNickname.Name = "lblNickname";
            lblNickname.Size = new Size(118, 28);
            lblNickname.TabIndex = 4;
            lblNickname.Text = "Khách";
            lblNickname.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // tblBody
            // 
            tblBody.ColumnCount = 2;
            tblBody.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220F));
            tblBody.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblBody.Controls.Add(pnlSidebar, 0, 0);
            tblBody.Controls.Add(pnlContent, 1, 0);
            tblBody.Dock = DockStyle.Fill;
            tblBody.Location = new Point(0, 76);
            tblBody.Margin = new Padding(0);
            tblBody.Name = "tblBody";
            tblBody.RowCount = 1;
            tblBody.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tblBody.Size = new Size(1200, 575);
            tblBody.TabIndex = 1;
            // 
            // pnlSidebar
            // 
            pnlSidebar.BackColor = Color.White;
            pnlSidebar.Controls.Add(flpNavigation);
            pnlSidebar.Controls.Add(btnDisconnect);
            pnlSidebar.Controls.Add(lblNavigation);
            pnlSidebar.Dock = DockStyle.Fill;
            pnlSidebar.Location = new Point(0, 1);
            pnlSidebar.Margin = new Padding(0, 1, 1, 0);
            pnlSidebar.Name = "pnlSidebar";
            pnlSidebar.Padding = new Padding(16, 18, 16, 16);
            pnlSidebar.Size = new Size(209, 574);
            pnlSidebar.TabIndex = 0;
            // 
            // flpNavigation
            // 
            flpNavigation.AutoScroll = true;
            flpNavigation.Controls.Add(btnHome);
            flpNavigation.Controls.Add(btnOnlinePlayers);
            flpNavigation.Controls.Add(btnInvitations);
            flpNavigation.Controls.Add(btnMatches);
            flpNavigation.Controls.Add(btnGame);
            flpNavigation.Controls.Add(btnConnect);
            flpNavigation.Dock = DockStyle.Fill;
            flpNavigation.FlowDirection = FlowDirection.TopDown;
            flpNavigation.Location = new Point(12, 50);
            flpNavigation.Margin = new Padding(0);
            flpNavigation.Name = "flpNavigation";
            flpNavigation.Size = new Size(185, 466);
            flpNavigation.TabIndex = 0;
            flpNavigation.WrapContents = false;
            // 
            // btnHome
            // 
            btnHome.BackColor = Color.FromArgb(235, 242, 255);
            btnHome.FlatAppearance.BorderSize = 0;
            btnHome.FlatStyle = FlatStyle.Flat;
            btnHome.ForeColor = Color.FromArgb(37, 99, 235);
            btnHome.Location = new Point(0, 0);
            btnHome.Margin = new Padding(0, 0, 0, 8);
            btnHome.Name = "btnHome";
            btnHome.Padding = new Padding(12, 0, 0, 0);
            btnHome.Size = new Size(180, 46);
            btnHome.TabIndex = 0;
            btnHome.Tag = "Home";
            btnHome.Text = "Trang chủ";
            btnHome.TextAlign = ContentAlignment.MiddleLeft;
            btnHome.UseVisualStyleBackColor = false;
            btnHome.Click += NavigationButton_Click;
            // 
            // btnOnlinePlayers
            // 
            btnOnlinePlayers.BackColor = Color.White;
            btnOnlinePlayers.FlatAppearance.BorderSize = 0;
            btnOnlinePlayers.FlatStyle = FlatStyle.Flat;
            btnOnlinePlayers.ForeColor = Color.FromArgb(23, 43, 77);
            btnOnlinePlayers.Location = new Point(0, 54);
            btnOnlinePlayers.Margin = new Padding(0, 0, 0, 8);
            btnOnlinePlayers.Name = "btnOnlinePlayers";
            btnOnlinePlayers.Padding = new Padding(12, 0, 0, 0);
            btnOnlinePlayers.Size = new Size(180, 46);
            btnOnlinePlayers.TabIndex = 1;
            btnOnlinePlayers.Tag = "OnlinePlayers";
            btnOnlinePlayers.Text = "Người chơi trực tuyến";
            btnOnlinePlayers.TextAlign = ContentAlignment.MiddleLeft;
            btnOnlinePlayers.UseVisualStyleBackColor = false;
            btnOnlinePlayers.Click += NavigationButton_Click;
            // 
            // btnInvitations
            // 
            btnInvitations.BackColor = Color.White;
            btnInvitations.FlatAppearance.BorderSize = 0;
            btnInvitations.FlatStyle = FlatStyle.Flat;
            btnInvitations.ForeColor = Color.FromArgb(23, 43, 77);
            btnInvitations.Location = new Point(0, 108);
            btnInvitations.Margin = new Padding(0, 0, 0, 8);
            btnInvitations.Name = "btnInvitations";
            btnInvitations.Padding = new Padding(12, 0, 0, 0);
            btnInvitations.Size = new Size(180, 46);
            btnInvitations.TabIndex = 2;
            btnInvitations.Tag = "Invitations";
            btnInvitations.Text = "Lời mời";
            btnInvitations.TextAlign = ContentAlignment.MiddleLeft;
            btnInvitations.UseVisualStyleBackColor = false;
            btnInvitations.Click += NavigationButton_Click;
            // 
            // btnMatches
            // 
            btnMatches.BackColor = Color.White;
            btnMatches.FlatAppearance.BorderSize = 0;
            btnMatches.FlatStyle = FlatStyle.Flat;
            btnMatches.ForeColor = Color.FromArgb(23, 43, 77);
            btnMatches.Location = new Point(0, 162);
            btnMatches.Margin = new Padding(0, 0, 0, 8);
            btnMatches.Name = "btnMatches";
            btnMatches.Padding = new Padding(12, 0, 0, 0);
            btnMatches.Size = new Size(180, 46);
            btnMatches.TabIndex = 3;
            btnMatches.Tag = "Matches";
            btnMatches.Text = "Trận đang chơi";
            btnMatches.TextAlign = ContentAlignment.MiddleLeft;
            btnMatches.UseVisualStyleBackColor = false;
            btnMatches.Click += NavigationButton_Click;
            // 
            // btnGame
            // 
            btnGame.BackColor = Color.White;
            btnGame.Enabled = false;
            btnGame.FlatAppearance.BorderSize = 0;
            btnGame.FlatStyle = FlatStyle.Flat;
            btnGame.ForeColor = Color.FromArgb(23, 43, 77);
            btnGame.Location = new Point(0, 216);
            btnGame.Margin = new Padding(0, 0, 0, 8);
            btnGame.Name = "btnGame";
            btnGame.Padding = new Padding(12, 0, 0, 0);
            btnGame.Size = new Size(180, 46);
            btnGame.TabIndex = 4;
            btnGame.Tag = "Game";
            btnGame.Text = "Phòng của tôi";
            btnGame.TextAlign = ContentAlignment.MiddleLeft;
            btnGame.UseVisualStyleBackColor = false;
            btnGame.Click += NavigationButton_Click;
            // 
            // btnConnect
            // 
            btnConnect.BackColor = Color.White;
            btnConnect.FlatAppearance.BorderSize = 0;
            btnConnect.FlatStyle = FlatStyle.Flat;
            btnConnect.ForeColor = Color.FromArgb(23, 43, 77);
            btnConnect.Location = new Point(0, 270);
            btnConnect.Margin = new Padding(0, 0, 0, 8);
            btnConnect.Name = "btnConnect";
            btnConnect.Padding = new Padding(12, 0, 0, 0);
            btnConnect.Size = new Size(180, 46);
            btnConnect.TabIndex = 5;
            btnConnect.Text = "Kết nối";
            btnConnect.TextAlign = ContentAlignment.MiddleLeft;
            btnConnect.UseVisualStyleBackColor = false;
            // 
            // btnDisconnect
            // 
            btnDisconnect.BackColor = Color.White;
            btnDisconnect.Dock = DockStyle.Bottom;
            btnDisconnect.Enabled = false;
            btnDisconnect.FlatAppearance.BorderColor = Color.FromArgb(220, 38, 38);
            btnDisconnect.FlatStyle = FlatStyle.Flat;
            btnDisconnect.ForeColor = Color.FromArgb(220, 38, 38);
            btnDisconnect.Location = new Point(12, 516);
            btnDisconnect.Name = "btnDisconnect";
            btnDisconnect.Size = new Size(185, 42);
            btnDisconnect.TabIndex = 1;
            btnDisconnect.Text = "Ngắt kết nối";
            btnDisconnect.UseVisualStyleBackColor = false;
            // 
            // lblNavigation
            // 
            lblNavigation.Dock = DockStyle.Top;
            lblNavigation.Font = new Font("Segoe UI", 9F);
            lblNavigation.ForeColor = Color.FromArgb(100, 116, 139);
            lblNavigation.Location = new Point(12, 16);
            lblNavigation.Name = "lblNavigation";
            lblNavigation.Size = new Size(185, 34);
            lblNavigation.TabIndex = 2;
            lblNavigation.Text = "ĐIỀU HƯỚNG";
            // 
            // pnlContent
            // 
            pnlContent.BackColor = Color.FromArgb(245, 247, 251);
            pnlContent.Dock = DockStyle.Fill;
            pnlContent.Location = new Point(210, 0);
            pnlContent.Margin = new Padding(0);
            pnlContent.Name = "pnlContent";
            pnlContent.Padding = new Padding(26);
            pnlContent.Size = new Size(990, 575);
            pnlContent.TabIndex = 1;
            // 
            // statusMain
            // 
            statusMain.AutoSize = false;
            statusMain.BackColor = Color.FromArgb(248, 250, 252);
            statusMain.Dock = DockStyle.Fill;
            statusMain.ImageScalingSize = new Size(20, 20);
            statusMain.Items.AddRange(new ToolStripItem[] { lblServer, lblStatusSpacer });
            statusMain.Location = new Point(0, 647);
            statusMain.Name = "statusMain";
            statusMain.Padding = new Padding(16, 0, 16, 0);
            statusMain.Size = new Size(1200, 28);
            statusMain.SizingGrip = false;
            statusMain.TabIndex = 2;
            // 
            // lblServer
            // 
            lblServer.ForeColor = Color.FromArgb(100, 116, 139);
            lblServer.Name = "lblServer";
            lblServer.Size = new Size(154, 22);
            lblServer.Text = "Máy chủ: Chưa kết nối";
            // 
            // lblStatusSpacer
            // 
            lblStatusSpacer.Name = "lblStatusSpacer";
            lblStatusSpacer.Size = new Size(946, 22);
            lblStatusSpacer.Spring = true;
            // 
            // FMain
            // 
            AutoScaleDimensions = new SizeF(120F, 120F);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.FromArgb(220, 227, 237);
            ClientSize = new Size(1200, 675);
            Controls.Add(tblRoot);
            Font = new Font("Segoe UI", 10F);
            MinimumSize = new Size(1000, 650);
            Name = "FMain";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Caro LAN";
            tblRoot.ResumeLayout(false);
            pnlHeader.ResumeLayout(false);
            pnlHeader.PerformLayout();
            ((ISupportInitialize)pictureBox1).EndInit();
            tblBody.ResumeLayout(false);
            pnlSidebar.ResumeLayout(false);
            flpNavigation.ResumeLayout(false);
            statusMain.ResumeLayout(false);
            statusMain.PerformLayout();
            ResumeLayout(false);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
            }

            base.Dispose(disposing);
        }
        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>


        #endregion

        private PictureBox pictureBox1;
    }
}
