using CaroGame.Server;
using Microsoft.Extensions.Logging;

namespace Caro.Server.WinForms;

internal sealed class ServerForm : Form
{
    private readonly Button _toggleButton = new()
    {
        Text = "Chạy server",
        AutoSize = true,
        AccessibleName = "Chạy hoặc dừng server"
    };
    private readonly Label _status = new()
    {
        Text = "Đã dừng",
        AutoSize = true,
        Anchor = AnchorStyles.Left
    };
    private readonly RichTextBox _logs = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        WordWrap = false,
        Font = new Font(FontFamily.GenericMonospace, 9),
        AccessibleName = "Log server"
    };

    private CancellationTokenSource? _serverCancellation;
    private Task? _serverTask;
    private bool _closing;

    public ServerForm()
    {
        Text = "Caro Game Server";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(640, 420);
        ClientSize = new Size(840, 540);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 2,
            RowCount = 2
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(_toggleButton, 0, 0);
        layout.Controls.Add(_status, 1, 0);
        layout.Controls.Add(_logs, 0, 1);
        layout.SetColumnSpan(_logs, 2);
        Controls.Add(layout);

        _toggleButton.Click += async (_, _) =>
        {
            if (_serverTask is null)
                StartServer();
            else
                await StopServerAsync();
        };
    }

    private void StartServer()
    {
        _serverCancellation = new CancellationTokenSource();
        var cancellation = _serverCancellation;
        _toggleButton.Text = "Dừng server";
        _status.Text = "Đang chạy · TCP 5000 · UDP broadcast 5001";
        AppendLog("Đang khởi động server...");

        _serverTask = Task.Run(() => ServerHost.RunAsync(cancellation.Token,
            logging => logging.AddProvider(new UiLoggerProvider(AppendLog))));
        _ = ObserveServerAsync(_serverTask);
    }

    private async Task StopServerAsync()
    {
        _toggleButton.Enabled = false;
        _status.Text = "Đang dừng...";
        _serverCancellation?.Cancel();
        try
        {
            if (_serverTask is not null)
                await _serverTask;
        }
        catch
        {
            // ObserveServerAsync displays the failure in the log.
        }
    }

    private async Task ObserveServerAsync(Task task)
    {
        var failed = false;
        try
        {
            await task;
        }
        catch (Exception exception)
        {
            failed = true;
            AppendLog($"Server lỗi: {exception}");
        }
        finally
        {
            _serverCancellation?.Dispose();
            _serverCancellation = null;
            _serverTask = null;
            if (!IsDisposed)
            {
                _status.Text = failed ? "Có lỗi · xem log" : "Đã dừng";
                _toggleButton.Text = "Chạy server";
                _toggleButton.Enabled = true;
            }
        }
    }

    private void AppendLog(string line)
    {
        if (IsDisposed || !IsHandleCreated) return;
        if (InvokeRequired)
        {
            try { BeginInvoke((Action)(() => AppendLog(line))); }
            catch (InvalidOperationException) { }
            return;
        }

        const int maxCharacters = 150_000;
        if (_logs.TextLength > maxCharacters)
            _logs.Text = _logs.Text[(_logs.TextLength / 2)..];
        _logs.AppendText(line + Environment.NewLine);
        _logs.SelectionStart = _logs.TextLength;
        _logs.ScrollToCaret();
    }

    protected override async void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_closing && _serverTask is { IsCompleted: false })
        {
            e.Cancel = true;
            _closing = true;
            await StopServerAsync();
            Close();
            return;
        }

        base.OnFormClosing(e);
    }
}
