using System.Diagnostics;
using System.Windows.Threading;
using UI_Framework;
using static UI_Framework.UI;

namespace StressLab;

public sealed class VisualStressPanel : LabComponent
{
    protected override View Render()
    {
        var run = Model.VisualStress.Value;
        var progress = 500d * run.Step / run.Total;
        return VStack(
            HStack(Text(run.Running ? $"LIVE · {run.Step}/{run.Total}" : "VISUAL STRESS").FontSize(16).Width(175), Text(run.Action).FontSize(14).Width(980)).Spacing(12),
            HStack(Enumerable.Range(0, 8).Select(index =>
                Text(new[] { "EDIT", "TOGGLE", "SELECT", "REORDER", "LAYOUT", "NESTED", "BURST", "FILTER" }[index])
                    .FontSize(12).Padding(8).Width(140).Background(run.Running && index == (run.Step - 1) % 8 ? "#2563EB" : "#E2E8F0")
                    .Foreground(run.Running && index == (run.Step - 1) % 8 ? "#FFFFFF" : "#475569").CornerRadius(6)
            ).ToArray()).Spacing(8),
            HStack(Text("").Width(progress).Height(5).Background("#2563EB"), Text("").Width(500 - progress).Height(5).Background("#CBD5E1"),
                Caption("Changes demo data · 60 paced steps · Stop ends the run")).Spacing(0)
        ).Spacing(8).Padding(12).Background(run.Running ? "#DBEAFE" : "#E2E8F0").CornerRadius(10);
    }
}
