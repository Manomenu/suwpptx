using Microsoft.Extensions.Logging;

namespace Suwpptx.Server.Tests;

internal sealed class CapturedLogs : ILoggerProvider, ISupportExternalScope
{
    private readonly List<CapturedLog> entries = [];
    private IExternalScopeProvider scopes = new LoggerExternalScopeProvider();

    public IReadOnlyList<CapturedLog> Entries
    {
        get
        {
            lock (entries)
            {
                return [.. entries];
            }
        }
    }

    public void SetScopeProvider(IExternalScopeProvider scopeProvider) => scopes = scopeProvider;

    public ILogger CreateLogger(string categoryName) => new Logger(this, categoryName);

    public void Dispose() { }

    private sealed class Logger(CapturedLogs owner, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => owner.scopes.Push(state);

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        )
        {
            string? traceId = null;
            owner.scopes.ForEachScope(
                (scope, _) =>
                {
                    if (scope is IEnumerable<KeyValuePair<string, object>> pairs)
                    {
                        traceId ??= pairs.Where(pair => pair.Key == "TraceId").Select(pair => pair.Value.ToString()).FirstOrDefault();
                    }
                },
                0
            );
            lock (owner.entries)
            {
                owner.entries.Add(new CapturedLog(category, logLevel, formatter(state, exception), traceId));
            }
        }
    }
}
