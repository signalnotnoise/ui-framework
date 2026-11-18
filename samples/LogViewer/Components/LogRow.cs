using UI_Framework;
using static UI_Framework.UI;

namespace LogViewer;

public sealed class LogRow : Component
{
    public LogWorkspace Model { get; set; } = null!;
    public LogEntry Entry { get; set; } = null!;
    public override View Body() => FlexRow(
        VStack(Text(Entry.Message).FontSize(13),
            HStack(Text(Entry.Timestamp.ToString("HH:mm:ss.fff")).FontSize(10).Width(80).Foreground(StudioTheme.Tokens.Muted),
                Text(Entry.Severity.ToUpperInvariant()).FontSize(10).Width(64).Foreground(StudioTheme.LevelColor(Entry.Severity)),
                Text(Entry.Source).FontSize(10).Foreground(StudioTheme.Tokens.Muted)).Spacing(8)).Spacing(5),
        Button(Model.SelectedId.Value == Entry.Id ? "Open" : "View", () => Model.SelectedId.Value = Entry.Id)
            .Width(72).Height(36).FontSize(12).ButtonStyle(ButtonStyleKind.Quiet).AccessibilityLabel($"Inspect event {Entry.Id}: {Entry.Message}"))
        .Spacing(8).Padding(10).Height(64)
        .Background(Model.SelectedId.Value == Entry.Id ? StudioTheme.Selection : Entry.Id % 2 == 0 ? StudioTheme.Panel : StudioTheme.Tokens.Canvas);
}
