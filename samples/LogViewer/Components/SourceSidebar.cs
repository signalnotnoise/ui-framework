using UI_Framework;
using static UI_Framework.UI;

namespace LogViewer;

public sealed class SourceSidebar : Component
{
    public LogWorkspace Model { get; set; } = null!;
    public override View Body() => Dock(
        VStack(new[] { Text("SOURCES").FontSize(11).Foreground(StudioTheme.Tokens.Muted),
            SourceButton("", "All services") }.Concat(DemoLogs.Sources.Select(source => SourceButton(source, source))).ToArray())
            .Spacing(9).Padding(12),
        bottom: VStack(Text("DEMO SESSION").FontSize(10).Foreground(StudioTheme.Tokens.Accent),
            Text("4 services").FontSize(13), Text($"{Model.Entries.Count:N0} buffered events").FontSize(12),
            Text("Bounded at 5,000.\nNo network connection.").FontSize(11).Foreground(StudioTheme.Tokens.Muted))
            .Spacing(10).Padding(14).Height(144)).Background(StudioTheme.Panel);

    private View SourceButton(string source, string label) => Button(label, () => Model.Source.Value = source)
        .Id(source.Length == 0 ? "all" : source).Height(36)
        .FontSize(12).ButtonStyle(Model.Source.Value == source ? ButtonStyleKind.Primary : ButtonStyleKind.Quiet)
        .AccessibilityLabel($"Filter source: {label}");
}
