using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace CaroGame.Server.Logging;

/// <summary>
/// Persists server logs as human-readable, daily files while allowing the console
/// provider to continue showing the same events in real time.
/// </summary>
public sealed class DailyFileLoggerProvider(string directory) : ILoggerProvider, ISupportExternalScope
{
    private static readonly JsonSerializerOptions PrettyJson = new() { WriteIndented = true };
    private readonly object _writeGate = new();
    private readonly string _directory = Path.GetFullPath(directory);
    private IExternalScopeProvider? _scopeProvider;
    private StreamWriter? _writer;
    private DateOnly? _currentDate;
    private bool _disposed;

    public ILogger CreateLogger(string categoryName) => new DailyFileLogger(this, categoryName);

    public void SetScopeProvider(IExternalScopeProvider scopeProvider) =>
        _scopeProvider = scopeProvider;

    internal void Write<TState>(
        string category,
        LogLevel level,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (_disposed || level == LogLevel.None)
            return;

        var timestamp = DateTimeOffset.Now;
        var message = formatter(state, exception);
        var fields = ExtractFields(state);
        var scopes = ExtractScopes();

        lock (_writeGate)
        {
            if (_disposed)
                return;

            try
            {
                EnsureWriter(timestamp);
                WriteEntry(timestamp, category, level, eventId, message, fields, scopes, exception);
                _writer!.Flush();
            }
            catch (IOException)
            {
                // Logging must never interrupt request processing.
            }
            catch (UnauthorizedAccessException)
            {
                // Logging must never interrupt request processing.
            }
        }
    }

    private void EnsureWriter(DateTimeOffset timestamp)
    {
        var date = DateOnly.FromDateTime(timestamp.LocalDateTime);
        if (_writer is not null && _currentDate == date)
            return;

        _writer?.Dispose();
        Directory.CreateDirectory(_directory);

        var path = Path.Combine(_directory, $"caro-server-{date:yyyy-MM-dd}.log");
        var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        var isEmpty = stream.Length == 0;
        _writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        _currentDate = date;

        if (isEmpty)
        {
            _writer.WriteLine("CARO GAME SERVER LOG");
            _writer.WriteLine($"File created: {timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}");
            _writer.WriteLine(new string('=', 100));
            _writer.WriteLine();
        }
    }

    private void WriteEntry(
        DateTimeOffset timestamp,
        string category,
        LogLevel level,
        EventId eventId,
        string message,
        IReadOnlyList<KeyValuePair<string, object?>> fields,
        IReadOnlyList<string> scopes,
        Exception? exception)
    {
        var titleSeparator = message.IndexOf(" | ", StringComparison.Ordinal);
        var title = titleSeparator >= 0 ? message[..titleSeparator] : message;
        var label = EventLabel(title, level);
        var shortCategory = category[(category.LastIndexOf('.') + 1)..];

        _writer!.WriteLine(new string('-', 100));
        _writer.WriteLine($"{timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}  [{LevelName(level),-5}]  [{label}]");
        _writer.WriteLine($"  Event       : {title}");
        _writer.WriteLine($"  Source      : {shortCategory}");

        if (eventId.Id != 0 || !string.IsNullOrWhiteSpace(eventId.Name))
            _writer.WriteLine($"  EventId     : {eventId.Id} ({eventId.Name ?? "unnamed"})");

        foreach (var field in fields)
        {
            if (field.Key.Equals("Payload", StringComparison.OrdinalIgnoreCase))
                WritePayload(field.Value);
            else
                WriteField(field.Key, FormatValue(field.Value));
        }

        if (scopes.Count > 0)
            WriteField("Scopes", string.Join(" => ", scopes));

        if (exception is not null)
        {
            _writer.WriteLine("  Exception   :");
            WriteIndentedLines(exception.ToString(), indentation: 4);
        }

        _writer.WriteLine();
    }

    private void WritePayload(object? value)
    {
        var payload = FormatValue(value);
        _writer!.WriteLine("  Payload     :");

        try
        {
            using var document = JsonDocument.Parse(payload);
            var prettyPayload = JsonSerializer.Serialize(document.RootElement, PrettyJson);
            WriteIndentedLines(prettyPayload, indentation: 4);
        }
        catch (JsonException)
        {
            WriteIndentedLines(payload, indentation: 4);
        }
    }

    private void WriteField(string name, string value)
    {
        if (!value.Contains('\n'))
        {
            _writer!.WriteLine($"  {name,-16}: {value}");
            return;
        }

        _writer!.WriteLine($"  {name,-16}:");
        WriteIndentedLines(value, indentation: 4);
    }

    private void WriteIndentedLines(string value, int indentation)
    {
        var prefix = new string(' ', indentation);
        foreach (var line in value.Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n'))
            _writer!.WriteLine($"{prefix}{line}");
    }

    private IReadOnlyList<string> ExtractScopes()
    {
        if (_scopeProvider is null)
            return [];

        var scopes = new List<string>();
        _scopeProvider.ForEachScope(static (scope, list) =>
        {
            var text = scope?.ToString();
            if (!string.IsNullOrWhiteSpace(text))
                list.Add(text);
        }, scopes);
        return scopes;
    }

    private static IReadOnlyList<KeyValuePair<string, object?>> ExtractFields<TState>(TState state)
    {
        if (state is not IEnumerable<KeyValuePair<string, object?>> values)
            return [];

        return values
            .Where(value => value.Key != "{OriginalFormat}")
            .ToArray();
    }

    private static string EventLabel(string title, LogLevel level)
    {
        if (title.StartsWith("TCP request received", StringComparison.Ordinal))
            return "INCOMING REQUEST";
        if (title.StartsWith("TCP outbound message sent", StringComparison.Ordinal))
            return "OUTGOING MESSAGE";
        if (title.StartsWith("TCP outbound message failed", StringComparison.Ordinal))
            return "OUTGOING FAILED";
        if (title.StartsWith("Request handled", StringComparison.Ordinal))
            return "REQUEST OK";
        if (title.StartsWith("Request rejected", StringComparison.Ordinal))
            return "REQUEST REJECTED";
        if (title.StartsWith("Request failed", StringComparison.Ordinal))
            return "REQUEST FAILED";
        if (title.Contains("connection", StringComparison.OrdinalIgnoreCase) ||
            title.Contains("connected", StringComparison.OrdinalIgnoreCase))
            return "CONNECTION";

        return level >= LogLevel.Error ? "SERVER ERROR" : "SERVER";
    }

    private static string LevelName(LogLevel level) => level switch
    {
        LogLevel.Trace => "TRACE",
        LogLevel.Debug => "DEBUG",
        LogLevel.Information => "INFO",
        LogLevel.Warning => "WARN",
        LogLevel.Error => "ERROR",
        LogLevel.Critical => "FATAL",
        _ => level.ToString().ToUpperInvariant()
    };

    private static string FormatValue(object? value) => value switch
    {
        null => "(null)",
        double number => number.ToString("0.##", CultureInfo.InvariantCulture),
        float number => number.ToString("0.##", CultureInfo.InvariantCulture),
        decimal number => number.ToString("0.##", CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };

    public void Dispose()
    {
        lock (_writeGate)
        {
            if (_disposed)
                return;

            _disposed = true;
            _writer?.Dispose();
            _writer = null;
        }
    }

    private sealed class DailyFileLogger(DailyFileLoggerProvider provider, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull =>
            provider._scopeProvider?.Push(state);

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            provider.Write(category, logLevel, eventId, state, exception, formatter);
        }
    }
}
