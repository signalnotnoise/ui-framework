using System.Diagnostics;
using System.Windows.Threading;
using UI_Framework;
using static UI_Framework.UI;

namespace StressLab;

public sealed class ItemEditor : LabComponent
{
    public WorkItem Item { get; set; } = null!;
    protected override View Render() => Card(
        Caption($"SHARED EDITOR / #{Item.Id:0000}"), Text("Work item details").FontSize(22),
        Caption("TITLE"), TextField(Item.Title),
        string.IsNullOrWhiteSpace(Item.Title.Value) ? Text("A title helps you find this item.").FontSize(12).Foreground("#B45309") : Caption("Editing this form updates the same row immediately."),
        HStack(VStack(Caption("OWNER"), TextField(Item.Owner)).Spacing(6).Width(205), VStack(Caption("ESTIMATE"), TextField(Item.Estimate)).Spacing(6).Width(105)).Spacing(12),
        Caption("NOTES / NESTED RECORD BINDING"), TextField(Item.Notes), Toggle("Completed", Item.Done),
        Model.ShowChecklist.Value ? Component<ChecklistPanel>(panel => { panel.Model = Model; panel.Item = Item; }).Id("checklist").Memo((Model, Item)) : Caption("Checklist hidden in project preferences."),
        Button("Remove work item", () => Model.Remove(Item))
    );
}
