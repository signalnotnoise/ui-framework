using UI_Framework;
using static UI_Framework.UI;

namespace LogViewer;

public sealed class LogToolbar : Component
{
    public LogWorkspace Model { get; set; } = null!;
    public override View Body() => FlexRow(
        Button(Model.SourcesHidden.Value ? "Show sources" : "Hide sources", () => Model.SourcesHidden.Value = !Model.SourcesHidden.Value)
            .Width(114).Height(36).FontSize(12).ButtonStyle(ButtonStyleKind.Quiet),
        VStack(Text("SEARCH MESSAGE, TRACE OR PAYLOAD").FontSize(10).Foreground(StudioTheme.Tokens.Muted),
            TextField(Model.Query).FontSize(13).AccessibilityLabel("Search logs").Height(36)).Spacing(3),
        VStack(Text("SEVERITY").FontSize(10).Foreground(StudioTheme.Tokens.Muted),
            Picker(DemoLogs.Severities, Model.Level).FontSize(13).AccessibilityLabel("Severity filter").Height(36)).Width(120).Spacing(3),
        Button("Clear filters", Model.ClearFilters).Width(94).Height(36).FontSize(12).ButtonStyle(ButtonStyleKind.Quiet),
        Button("Append 25", () => Model.Append()).Width(96).Height(36).FontSize(12),
        Button(Model.Streaming.Value ? "Pause stream" : "Start stream", () => Model.Streaming.Value = !Model.Streaming.Value)
            .Width(112).Height(36).FontSize(12).ButtonStyle(ButtonStyleKind.Primary))
        .Spacing(10).Padding(10).Height(80);
}
