using Microsoft.Extensions.Logging;

namespace Caro.Server.WinForms;

internal sealed class UiLoggerProvider(Action<string> writeLine) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new UiLogger(categoryName, writeLine);

    public void Dispose() { }

    private sealed class UiLogger(string categoryName, Action<string> writeLine) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            var category = categoryName[(categoryName.LastIndexOf('.') + 1)..];
            var line = $"{DateTime.Now:HH:mm:ss} [{logLevel}] {category}: {formatter(state, exception)}";
            if (exception is not null)
                line += Environment.NewLine + exception;
            writeLine(line);
        }
    }
}
