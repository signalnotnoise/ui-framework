using System.Diagnostics;
using System.Windows.Threading;
using UI_Framework;
using static UI_Framework.UI;

namespace StressLab;

public sealed class Dashboard : LabComponent
{
    protected override View Render() => VStack(
        HStack(
            VStack(Caption("UI FRAMEWORK / LIVE WORKSPACE"), Text("Binding stress lab").FontSize(30)).Spacing(4).Width(720),
            VStack(Text("State → binding → component → native UI").FontSize(16), Caption("Compare full and virtualized lists using the switch below.")).Spacing(8)
        ).Spacing(24),
        Child<CommandBar>("commands"),
        Child<VisualStressPanel>("visual-stress"),
        Child<SummaryPanel>("summary"),
        HStack(
            VStack(Child<FilterPanel>("filters"), Child<BoardSlot>("board")).Spacing(12).Width(820),
            VStack(Child<TelemetryPanel>("telemetry"),
                Scroll(VStack(Child<Inspector>("inspector"), Child<ProjectPanel>("project"), Child<DepthPanel>("depth"), Child<ActivityPanel>("activity")).Spacing(12)).Height(450)
            ).Spacing(12).Width(400)
        ).Spacing(20)
    ).Spacing(16).Padding(24).Background("#F1F5F9").Foreground("#0F172A");
}
