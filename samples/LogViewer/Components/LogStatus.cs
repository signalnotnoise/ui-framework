using UI_Framework;
using static UI_Framework.UI;

namespace LogViewer;

public sealed class LogStatus : Component
{
    public LogWorkspace Model { get; set; } = null!;
    public override View Body() => FlexRow(
        Text(Model.Streaming.Value ? "● SIMULATING" : "● PAUSED").FontSize(10).Width(112).Foreground(StudioTheme.Tokens.Accent),
        Text(Model.Feedback.Value).FontSize(11).Foreground(StudioTheme.Tokens.Muted),
        Text("UI FRAMEWORK  /  WPF").FontSize(10).Width(150).Foreground(StudioTheme.Tokens.Muted))
        .Padding(8).Height(32).Background(StudioTheme.Raised);
}
