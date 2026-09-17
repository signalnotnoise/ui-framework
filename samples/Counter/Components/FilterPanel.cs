using System.Diagnostics;
using System.Windows.Threading;
using UI_Framework;
using static UI_Framework.UI;

namespace StressLab;

public sealed class FilterPanel : LabComponent
{
    protected override View Render() => Card(
        HStack(Text("Find work").Width(100), TextField(Model.Query).Width(360), Toggle("Only open", Model.OnlyOpen)).Spacing(12),
        HStack(Button("Move first → last", Model.MoveFirstToLast), Button("Clear filters", () => { Model.Query.Value = ""; Model.OnlyOpen.Value = false; }),
            Toggle("Mount board", Model.BoardMounted), Toggle("Virtualized list", Model.Virtualized)).Spacing(12),
        Caption(Model.Virtualized.Value ? "Virtualized: visible rows + buffer. Offscreen component state is saved." : "Full list: every matching row has native controls.")
    );
}
