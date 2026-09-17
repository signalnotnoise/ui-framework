using System.Diagnostics;
using System.Windows.Threading;
using UI_Framework;
using static UI_Framework.UI;

namespace StressLab;

public sealed class ProjectPanel : LabComponent
{
    protected override View Render() => Card(Text("Project preferences").FontSize(20), TextField(Model.ProjectName), TextField(Model.ProjectOwner),
        Toggle("Compact every row", Model.CompactRows), Toggle("Show inspector checklist", Model.ShowChecklist),
        Caption("Both switches project into a nested immutable Preferences record."));
}
