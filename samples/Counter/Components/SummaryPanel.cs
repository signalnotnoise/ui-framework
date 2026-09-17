using System.Diagnostics;
using System.Windows.Threading;
using UI_Framework;
using static UI_Framework.UI;

namespace StressLab;

public sealed class SummaryPanel : LabComponent
{
    protected override View Render()
    {
        var items = Model.Items.ToArray();
        var completed = items.Count(item => item.Done.Value);
        return HStack(
            Card(Caption("WORK ITEMS"), Text(items.Length.ToString("N0")).FontSize(26)).Width(294),
            Card(Caption("OPEN"), Text((items.Length - completed).ToString("N0")).FontSize(26)).Width(294),
            Card(Caption("COMPLETED"), Text(completed.ToString("N0")).FontSize(26)).Width(294),
            Card(Caption("DATA FLOW"), Text("Two-way + projected").FontSize(22)).Width(310)
        ).Spacing(16);
    }
}
