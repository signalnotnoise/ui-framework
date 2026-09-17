using System.Diagnostics;
using System.Windows.Threading;
using UI_Framework;
using static UI_Framework.UI;

namespace StressLab;

public sealed class WorkRow : LabComponent
{
    public WorkItem Item { get; set; } = null!;
    private readonly State<bool> expanded = new(false);
    private Computed<bool>? isSelected;
    private State<WorkItem?>? selectionSource;
    protected override View Render()
    {
        var compact = Model.CompactRows.Value;
        if (!ReferenceEquals(selectionSource, Model.Selected))
        {
            selectionSource = Model.Selected;
            isSelected = selectionSource.Select(value => ReferenceEquals(value, Item));
        }
        var selected = isSelected!.Value;
        var showDetails = expanded.Value || Item.StressExpanded.Value;
        return VStack(
            HStack(Text($"#{Item.Id:0000}").FontSize(12).Width(58), TextField(Item.Title).Width(355),
                Toggle("Done", Item.Done).Width(78), Button("Inspect", () => Model.Selected.Value = Item),
                Button(showDetails ? "Less" : "More", () =>
                {
                    var wasOpen = expanded.Value || Item.StressExpanded.Value;
                    Item.StressExpanded.Value = false;
                    expanded.Value = !wasOpen;
                })).Spacing(10),
            compact ? Text("").FontSize(1) : HStack(Caption("OWNER").Width(58), TextField(Item.Owner).Width(170),
                Caption("Edits mirror in the inspector. Local expansion survives reorder.").Width(365)).Spacing(10),
            showDetails ? VStack(
                TextField(Item.Notes),
                Component<ChecklistPanel>(panel => { panel.Model = Model; panel.Item = Item; }).Id("checklist").Memo((Model, Item)),
                Button("Remove this work item", () => Model.Remove(Item))
            ).Spacing(10) : Text("").FontSize(1)
        ).Spacing(compact ? 2 : 8).Padding(12).Background(selected ? "#DBEAFE" : "#FFFFFF").CornerRadius(10);
    }
}
