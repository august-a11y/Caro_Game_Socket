using System.Diagnostics;
using Caro.Client.WinForms.Networking;
using CaroGame.Shared.Networking;

namespace Caro.Client.WinForms.Features.Session;

internal sealed class ConnectionDialog : Form
{
    private sealed record Announcement(ServerInfo Server, long LastSeen);
    private readonly ServerDiscoveryClient _discovery;
    private readonly string _previousHost;
    private readonly int _previousPort;
    private readonly bool _canReconnect;
    private readonly string _previousNickname;
    private readonly Dictionary<string, ListViewItem> _servers = new(StringComparer.OrdinalIgnoreCase);
    private readonly ListView _list = new()
    {
        Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true,
        MultiSelect = false, HideSelection = false, AccessibleName = "Máy chủ trong mạng LAN"
    };
    private readonly Label _status = new() { Dock = DockStyle.Fill, AutoSize = true };
    private readonly TextBox _nickname = new()
    {
        Dock = DockStyle.Fill, Enabled = false,
        PlaceholderText = "Nhập tên người chơi", AccessibleName = "Tên người chơi"
    };
    private readonly CheckBox _reconnect = new() { Text = "Kết nối lại phiên trước trên máy chủ đã chọn", AutoSize = true, Enabled = false };
    private readonly Button _connect = new() { Text = "Kết nối", AutoSize = true, Enabled = false };
    private readonly System.Windows.Forms.Timer _refresh = new() { Interval = 1000 };
    private bool _discoveryFailed;
    private bool _closed;
    private bool _attempting;

    public ServerInfo? SelectedServer { get; private set; }
    public event Func<ServerInfo, string, bool, Task<bool>>? ConnectionRequested;
    public string Nickname => _nickname.Text.Trim();
    public bool Reconnect => _reconnect.Checked;

    public ConnectionDialog(ServerDiscoveryClient discovery, string host, int port, string nickname, bool canReconnect)
    {
        _discovery = discovery;
        _previousHost = host;
        _previousPort = port;
        _canReconnect = canReconnect;
        _previousNickname = nickname;
        Text = "Chọn máy chủ Caro";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ClientSize = new Size(680, 430);
        _list.Columns.Add("Tên máy chủ", 620);
        _list.SelectedIndexChanged += (_, _) => SelectionChanged();
        _nickname.TextChanged += (_, _) =>
        {
            UpdateReconnectAvailability();
            UpdateConnectButton();
        };
        _reconnect.CheckedChanged += (_, _) =>
        {
            UpdateConnectButton();
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 2, RowCount = 6
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        var instruction = new Label { Text = "Chọn máy chủ trong LAN, nhập tên người chơi, sau đó nhấn Kết nối.", AutoSize = true };
        layout.Controls.Add(instruction, 0, 0);
        layout.SetColumnSpan(instruction, 2);
        layout.Controls.Add(_list, 0, 1);
        layout.SetColumnSpan(_list, 2);
        layout.Controls.Add(_status, 0, 2);
        layout.SetColumnSpan(_status, 2);
        layout.Controls.Add(new Label { Text = "Tên người chơi", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 3);
        layout.Controls.Add(_nickname, 1, 3);
        layout.Controls.Add(_reconnect, 1, 4);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var cancel = new Button { Text = "Hủy", DialogResult = DialogResult.Cancel, AutoSize = true };
        _connect.Click += async (_, _) =>
        {
            RemoveExpiredServers();
            if (_attempting || !_connect.Enabled || CurrentServer is not { } server) return;
            _attempting = true;
            _connect.Enabled = false;
            _status.ForeColor = Color.FromArgb(100, 116, 139);
            _status.Text = "Đang kết nối đến máy chủ...";
            try
            {
                if (ConnectionRequested is not null && await ConnectionRequested(server, Nickname, Reconnect))
                {
                    SelectedServer = server;
                    DialogResult = DialogResult.OK;
                }
            }
            catch (Exception error) { DisplayConnectionError(error.Message); }
            finally
            {
                _attempting = false;
                if (!IsDisposed && DialogResult != DialogResult.OK) UpdateConnectButton();
            }
            return;
        };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(_connect);
        layout.Controls.Add(buttons, 0, 5);
        layout.SetColumnSpan(buttons, 2);
        Controls.Add(layout);
        AcceptButton = _connect;
        CancelButton = cancel;
        _refresh.Tick += (_, _) => RemoveExpiredServers();
        _discovery.ServerDiscovered += ServerDiscovered;
        _discovery.DiscoveryError += DiscoveryError;
    }

    private Announcement? CurrentAnnouncement =>
        _list.SelectedItems.Count == 1 ? _list.SelectedItems[0].Tag as Announcement : null;

    private ServerInfo? CurrentServer => _discoveryFailed ? null : CurrentAnnouncement?.Server;

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        UpdateStatus();
        try
        {
            _discovery.Start();
            _refresh.Start();
        }
        catch (Exception error) { ShowDiscoveryError(error); }
    }

    private void ServerDiscovered(ServerInfo server) => PostToUi(() =>
    {
        string key = $"{server.Host}:{server.TcpPort}";
        var announcement = new Announcement(server, Stopwatch.GetTimestamp());
        string displayName = string.IsNullOrWhiteSpace(server.ServerName) ? "Máy chủ Caro" : server.ServerName.Trim();
        if (!_servers.TryGetValue(key, out var item))
        {
            item = new ListViewItem(displayName) { Tag = announcement };
            _servers.Add(key, item);
            _list.Items.Add(item);
        }
        item.Tag = announcement;
        item.Text = displayName;
        UpdateStatus();
    });

    private void SelectionChanged()
    {
        _nickname.Clear();
        _nickname.Enabled = CurrentServer is not null;
        UpdateReconnectAvailability();
        UpdateConnectButton();
    }

    private void UpdateReconnectAvailability()
    {
        var server = CurrentServer;
        bool wasAvailable = _reconnect.Enabled;
        _reconnect.Enabled = _canReconnect && server is not null &&
            string.Equals(server.Host, _previousHost, StringComparison.OrdinalIgnoreCase) && server.TcpPort == _previousPort &&
            !string.IsNullOrWhiteSpace(Nickname) && string.Equals(Nickname, _previousNickname, StringComparison.Ordinal);
        if (!_reconnect.Enabled) _reconnect.Checked = false;
        else if (!wasAvailable) _reconnect.Checked = true;
    }

    private void UpdateConnectButton() => _connect.Enabled =
        CurrentServer is not null && !string.IsNullOrWhiteSpace(Nickname);

    private void RemoveExpiredServers()
    {
        foreach (var (key, item) in _servers.ToArray())
        {
            var age = Stopwatch.GetElapsedTime(((Announcement)item.Tag!).LastSeen);
            if (age >= TimeSpan.FromSeconds(10))
            {
                _servers.Remove(key);
                _list.Items.Remove(item);
            }
        }
        UpdateConnectButton();
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        if (_discoveryFailed) return;
        _status.Text = _servers.Count == 0
            ? "Đang tìm máy chủ... Hãy bật máy chủ trong cùng mạng LAN."
            : $"Tìm thấy {_servers.Count} máy chủ. Chọn máy chủ và nhập tên người chơi để kết nối.";
    }

    private void DiscoveryError(Exception error) => PostToUi(() => ShowDiscoveryError(error));

    private void ShowDiscoveryError(Exception error)
    {
        _discoveryFailed = true;
        _nickname.Enabled = CurrentServer is not null;
        _status.Text =
            $"Không thể tìm máy chủ trong mạng LAN: {error.Message} Đóng cửa sổ và thử lại.";
        UpdateConnectButton();
    }

    public void DisplayConnectionError(string message)
    {
        if (_closed || IsDisposed) return;
        _status.ForeColor = Color.Firebrick;
        _status.Text = $"Kết nối không thành công: {message}";
    }

    private void PostToUi(Action action)
    {
        if (_closed || IsDisposed || !IsHandleCreated) return;
        try { BeginInvoke((Action)(() => { if (!_closed && !IsDisposed) action(); })); }
        catch (InvalidOperationException) { }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _closed = true;
        _refresh.Stop();
        base.OnFormClosed(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _closed = true;
            _refresh.Dispose();
            _discovery.ServerDiscovered -= ServerDiscovered;
            _discovery.DiscoveryError -= DiscoveryError;
        }
        base.Dispose(disposing);
    }
}
