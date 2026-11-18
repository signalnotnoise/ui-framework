using UI_Framework;

namespace LogViewer;

public sealed class LogWorkspace
{
    public const int Capacity = 5000;
    private long sequence;
    public StateList<LogEntry> Entries { get; } = new();
    public State<string> Query { get; } = new("");
    public State<int> Level { get; } = new(0);
    public State<string> Source { get; } = new("");
    public State<long> SelectedId { get; } = new(0);
    public State<bool> NewestFirst { get; } = new(true);
    public State<bool> Streaming { get; } = new(false);
    public State<bool> SourcesHidden { get; } = new(false);
    public State<double> SidebarWidth { get; } = new(184);
    public State<double> StreamWidth { get; } = new(750);
    public State<double> ViewportHeight { get; } = new(820);
    public State<double> ViewportWidth { get; } = new(1440);
    public State<string> Feedback { get; } = new("Ready · simulated data only");
    public Computed<IReadOnlyList<LogEntry>> Visible { get; }
    public Action<string> CopyText { get; set; } = _ => { };

    public LogWorkspace()
    {
        Visible = new(() => {
            var query = Query.Value.Trim();
            var level = Level.Value;
            var source = Source.Value;
            var filtered = Entries.Where(entry => (source.Length == 0 || entry.Source == source)
                && (level <= 0 || entry.Severity == DemoLogs.Severities[level])
                && (query.Length == 0 || ($"{entry.Message} {entry.Source} {entry.TraceId} {entry.Payload}")
                    .Contains(query, StringComparison.OrdinalIgnoreCase)));
            return (NewestFirst.Value ? filtered.Reverse() : filtered).ToArray();
        });
        Reset();
    }

    public LogEntry? Selected { get { var id = SelectedId.Value; return Entries.FirstOrDefault(entry => entry.Id == id); } }
    public double ListHeight => Math.Max(120, ViewportHeight.Value - 292);
    public Binding<double> SplitWidth => new(
        () => Math.Clamp(StreamWidth.Value, 340, Math.Max(340, ViewportWidth.Value - (SourcesHidden.Value ? 0 : SidebarWidth.Value + 6) - 306)),
        value => StreamWidth.Value = value);

    public void Reset()
    {
        Streaming.Value = false;
        sequence = 2380;
        Entries.ReplaceAll(Enumerable.Range(1, (int)sequence).Select(i => DemoLogs.Create(i)));
        ClearFilters();
        NewestFirst.Value = true;
        SelectedId.Value = sequence;
        Feedback.Value = "Demo reset · 2,380 events";
    }

    public void ClearFilters() { Query.Value = ""; Level.Value = 0; Source.Value = ""; }
    public void Append(int count = 25)
    {
        var batch = Enumerable.Range(0, count).Select(_ => DemoLogs.Create(++sequence)).ToArray();
        Entries.ReplaceAll(Entries.Concat(batch).TakeLast(Capacity).ToArray());
        if (Selected is null) SelectedId.Value = Entries[0].Id;
        Feedback.Value = $"Added {count} simulated events · buffer limit {Capacity:N0}";
    }
    public void CopySelected()
    {
        if (Selected is not { } entry) return;
        CopyText(entry.Payload);
    }
}
