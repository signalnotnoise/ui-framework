using UI_Framework;
using static UI_Framework.UI;

namespace LogViewer;

public sealed class LogScreen : Component
{
    public LogWorkspace Model { get; set; } = null!;
    public override View Body() => Dock(
        SplitPane(Component<SourceSidebar>(c => c.Model = Model).Id("sources"),
            SplitPane(Component<LogStream>(c => c.Model = Model).Id("stream"),
                Component<LogInspector>(c => c.Model = Model).Id("inspector"), Model.SplitWidth,
                minimumFirst: 340, minimumSecond: 300).Id("workspace"),
            Model.SidebarWidth, minimumFirst: 160, minimumSecond: 646, firstCollapsed: Model.SourcesHidden.Value),
        top: VStack(
            FlexRow(VStack(Text("TRACE / EVENT WORKSPACE").FontSize(19),
                    Text("Local session   /   sample-project").FontSize(11).Foreground(StudioTheme.Tokens.Muted)).Spacing(4),
                Text("SIMULATED DATA").FontSize(11).Foreground(StudioTheme.Tokens.Accent).Width(134),
                Button("Reset demo", Model.Reset).Width(100).Height(34).FontSize(12).ButtonStyle(ButtonStyleKind.Quiet))
                .Spacing(16).Padding(12).Height(72).Background(StudioTheme.Panel),
            Component<LogToolbar>(c => c.Model = Model).Id("toolbar")).Height(152),
        bottom: Component<LogStatus>(c => c.Model = Model).Id("status").Height(32))
        .Foreground(StudioTheme.Tokens.Ink).Background(StudioTheme.Tokens.Canvas);
}
