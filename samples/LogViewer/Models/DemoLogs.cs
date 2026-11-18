using System.Text.Json;

namespace LogViewer;

public static class DemoLogs
{
    public static readonly string[] Sources = ["gateway", "worker", "storage", "identity"];
    public static readonly string[] Severities = ["All levels", "Error", "Warning", "Info", "Debug"];
    private static readonly string[] Messages = ["Request completed", "Job acknowledged", "Snapshot committed", "Token refreshed",
        "Upstream response delayed", "Retry scheduled", "Cache entry expired", "Session renewed",
        "Upstream request timed out", "Job retry limit reached", "Replica connection lost", "Access denied"];

    public static LogEntry Create(long id)
    {
        var source = Sources[(int)(id % 4)];
        var severity = id % 17 == 0 ? "Error" : id % 7 == 0 ? "Warning" : id % 5 == 0 ? "Debug" : "Info";
        var message = Messages[(severity == "Error" ? 8 : severity == "Warning" ? 4 : 0) + (int)(id % 4)];
        var timestamp = new DateTimeOffset(2026, 11, 15, 14, 0, 0, TimeSpan.Zero).AddMilliseconds(id * 137);
        var trace = $"tr-{id / 4:x8}";
        var payload = JsonSerializer.Serialize(new {
            timestamp, level = severity.ToLowerInvariant(), service = source,
            message, trace_id = trace, event_id = $"evt-{id:000000}",
            environment = "local-demo", duration_ms = severity == "Error" ? 5000 : (id * 13) % 240,
            request = new { method = "GET", route = "/v1/inventory", region = "us-east-1" },
            detail = severity == "Error" ? "Simulated upstream failure. Retry budget exhausted." : "Generated sample event. No external connection."
        }, new JsonSerializerOptions { WriteIndented = true });
        return new(id, timestamp, severity, source, message, trace, payload);
    }
}
