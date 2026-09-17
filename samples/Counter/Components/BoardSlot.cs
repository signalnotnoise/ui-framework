using System.Diagnostics;
using System.Windows.Threading;
using UI_Framework;
using static UI_Framework.UI;

namespace StressLab;

public sealed class BoardSlot : LabComponent
{
    protected override View Render() => Model.BoardMounted.Value
        ? Child<WorkList>("list")
        : Card(Text("Board unmounted").FontSize(22), Text("Item data stays in the workspace. Component-local expansion state resets when mounted again."));
}
