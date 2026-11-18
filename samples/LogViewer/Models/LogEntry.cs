namespace LogViewer;

public sealed record LogEntry(long Id, DateTimeOffset Timestamp, string Severity,
    string Source, string Message, string TraceId, string Payload);
