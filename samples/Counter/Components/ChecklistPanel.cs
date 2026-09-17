using System.Diagnostics;
using System.Windows.Threading;
using UI_Framework;
using static UI_Framework.UI;

namespace StressLab;

public sealed class ChecklistPanel : LabComponent
{
    public WorkItem Item { get; set; } = null!;
    protected override View Render() => VStack(
        Caption($"CHECKLIST / {Item.Checklist.Count} ITEMS"),
        VStack(Item.Checklist.Select(step => Component<ChecklistRow>(row => { row.Model = Model; row.Step = step; row.Remove = () => Item.Checklist.Remove(step); }).Id(step.Id).Memo((Model, Item, step))).ToArray()).Spacing(8),
        Button("+ Checklist item", () => Item.Checklist.Add(new("New acceptance check")))
    ).Spacing(8);
}
