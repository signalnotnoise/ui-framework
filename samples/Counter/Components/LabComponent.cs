using System.Diagnostics;
using System.Windows.Threading;
using UI_Framework;
using static UI_Framework.UI;

namespace StressLab;

public abstract class LabComponent : Component
{
    public WorkspaceModel Model { get; set; } = null!;
    public sealed override View Body() { Model.Counters.Builds++; return Render(); }
    protected abstract View Render();
    protected View Child<T>(string key) where T : LabComponent, new() => Component<T>(view => view.Model = Model).Id(key).Memo(Model);
    public override void OnMounted() => Model.Counters.Mounts++;
    public override void OnUnmounted() => Model.Counters.Unmounts++;
    protected static View Card(params View[] children) => VStack(children).Spacing(10).Padding(16).Background("#FFFFFF").CornerRadius(12);
    protected static View Caption(string text) => Text(text).FontSize(12).Foreground("#64748B");
}
