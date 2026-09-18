using System.Diagnostics;
using System.Text;
using System.Threading.Channels;

namespace Caro.Client.WinForms.Core;

internal sealed class ClientLogger
{
    private readonly string _filePath;
    private readonly Channel<string> _entries = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false,
        AllowSynchronousContinuations = false
    });
    private readonly Task _writer;
    private long _queued;
    private long _written;

    public static ClientLogger Shared { get; } = new();
    public string FilePath => _filePath;

    private ClientLogger()
    {
        _filePath = CreateLogPath();
        _writer = Task.Run(WriteLoopAsync);
    }

    public void Debug(string message) => Write("DBG", message);
    public void Info(string message) => Write("INF", message);
    public void Warning(string message, Exception? exception = null) => Write("WRN", message, exception);
    public void Error(string message, Exception? exception = null) => Write("ERR", message, exception);

    private void Write(string level, string message, Exception? exception = null)
    {
        try
        {
            var builder = new StringBuilder()
                .Append(DateTimeOffset.Now.ToString("O"))
                .Append(" [T").Append(Environment.CurrentManagedThreadId).Append("] ")
                .Append(level).Append(' ').AppendLine(message);
            if (exception is not null) builder.AppendLine(exception.ToString());
            var line = builder.ToString();
            Debugger.Log(0, "Caro.Client", line);
            Interlocked.Increment(ref _queued);
            if (!_entries.Writer.TryWrite(line))
                Interlocked.Increment(ref _written);
        }
        catch
        {
            // Logging must never bring down the client.
        }
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        var target = Volatile.Read(ref _queued);
        while (Volatile.Read(ref _written) < target)
            await Task.Delay(10, cancellationToken).ConfigureAwait(false);
    }

    private async Task WriteLoopAsync()
    {
        await foreach (var line in _entries.Reader.ReadAllAsync())
        {
            try
            {
                RotateIfNeeded();
                await File.AppendAllTextAsync(_filePath, line, Encoding.UTF8).ConfigureAwait(false);
            }
            catch
            {
                // Logging must never bring down the client.
            }
            finally { Interlocked.Increment(ref _written); }
        }
    }

    private void RotateIfNeeded()
    {
        const long maxBytes = 5 * 1024 * 1024;
        if (!File.Exists(_filePath) || new FileInfo(_filePath).Length < maxBytes) return;
        try
        {
            var archive = _filePath + ".1";
            File.Delete(archive);
            File.Move(_filePath, archive);
        }
        catch { }
    }

    private static string CreateLogPath()
    {
        try
        {
            var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(root)) root = Path.GetTempPath();
            var directory = Path.Combine(root, "CaroGame");
            Directory.CreateDirectory(directory);
            return Path.Combine(directory, "client.log");
        }
        catch { return Path.Combine(Path.GetTempPath(), "caro-client.log"); }
    }
}
