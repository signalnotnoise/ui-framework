using System.Diagnostics;
using System.Windows.Threading;
using UI_Framework;
using static UI_Framework.UI;

namespace StressLab;

public sealed class Inspector : LabComponent
{
    protected override View Render()
    {
        var item = Model.Selected.Value;
        return item is null ? Card(Text("Inspector").FontSize(22), Text("Add or select a work item."))
            : Component<ItemEditor>(editor => { editor.Model = Model; editor.Item = item; }).Id(item.Id.ToString()).Memo((Model, item));
    }
}
