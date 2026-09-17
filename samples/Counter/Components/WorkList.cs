using System.Diagnostics;
using System.Windows.Threading;
using UI_Framework;
using static UI_Framework.UI;

namespace StressLab;

public sealed class WorkList : LabComponent
{
    protected override View Render()
    {
        var query = Model.Query.Value.Trim();
        var onlyOpen = Model.OnlyOpen.Value;
        var rows = Model.Items.Where(item => (!onlyOpen || !item.Done.Value)
            && (query.Length == 0 || item.Title.Value.Contains(query, StringComparison.OrdinalIgnoreCase))).ToArray();
        var descriptions = rows.Select(item =>
            Component<WorkRow>(row => { row.Model = Model; row.Item = item; }).Id(item.Id.ToString()).Memo((Model, item))).ToArray();
        if (descriptions.Length == 0)
            return Card(Text("No matching work items."));

        return Model.Virtualized.Value
            ? VirtualList(descriptions, 510).Spacing(10)
            : Scroll(VStack(descriptions).Spacing(10)).Height(510);
    }
}
