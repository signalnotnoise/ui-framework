using UI_Framework;
using static UI_Framework.UI;

namespace LogViewer;

public sealed class LogInspector : Component
{
    public LogWorkspace Model { get; set; } = null!;
    public override View Body()
    {
        var entry = Model.Selected;
        if (entry is null) return Text("Select an event to inspect.").Padding(18);
        return Dock(FlexColumn(
                VStack(Text(entry.Severity.ToUpperInvariant()).FontSize(11).Foreground(StudioTheme.LevelColor(entry.Severity)),
                    Text(entry.Message).FontSize(20),
                    Text($"{entry.Source}  /  evt-{entry.Id:000000}").FontSize(12).Foreground(StudioTheme.Tokens.Muted),
                    Text(entry.Timestamp.ToString("yyyy-MM-dd  HH:mm:ss.fff 'UTC'")).FontSize(12),
                    Text($"Trace  {entry.TraceId}").FontSize(12).Foreground(StudioTheme.Tokens.Muted),
                    Button("Filter this trace", () => { Model.ClearFilters(); Model.Query.Value = entry.TraceId; })
                        .FontSize(12).ButtonStyle(ButtonStyleKind.Quiet).Height(32),
                    Text("PAYLOAD / JSON").FontSize(11).Foreground(StudioTheme.Tokens.Accent)).Spacing(10).Padding(16).Height(254),
                TextEditor(new Binding<string>(() => entry.Payload, _ => { })).IsReadOnly(true)
                    .AccessibilityLabel("Selected event JSON payload").FontSize(12).Id("payload").Padding(12)),
            top: FlexRow(Text("Event inspector").FontSize(16),
                Button("Copy JSON", Model.CopySelected).Width(94).Height(32).FontSize(12).ButtonStyle(ButtonStyleKind.Quiet)).Padding(12).Height(72),
            bottom: Text("Selection stays available when filters change.").FontSize(10)
                .Foreground(StudioTheme.Tokens.Muted).Padding(10).Height(36)).Background(StudioTheme.Panel);
    }
}
