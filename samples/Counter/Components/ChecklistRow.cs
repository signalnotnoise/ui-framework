using System.Diagnostics;
using System.Windows.Threading;
using UI_Framework;
using static UI_Framework.UI;

namespace StressLab;

public sealed class ChecklistRow : LabComponent
{
    public ChecklistItem Step { get; set; } = null!;
    public Action Remove { get; set; } = null!;
    protected override View Render() => HStack(Toggle("", Step.Done).Width(22), TextField(Step.Title).Width(240), Button("×", Remove)).Spacing(6);
}
