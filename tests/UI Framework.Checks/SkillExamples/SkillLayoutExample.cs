using UI_Framework;
using static UI_Framework.UI;

public sealed class SkillLayoutExample : Component
{
    private readonly State<double> sidebarWidth = new(240);

    public override View Body() => Dock(
        SplitPane(VStack(Text("Sources"), Text("Local logs")),
            FlexColumn(Text("Entries").Height(24),
                FlexRow(Text("Message").Flex(2), Text("Severity"))),
            sidebarWidth, SplitAxis.Horizontal, minimumFirst: 180, minimumSecond: 320),
        top: HStack(Text("Log viewer"), Text("Ready")).Height(32),
        bottom: Text("Connected").Height(24));
}
