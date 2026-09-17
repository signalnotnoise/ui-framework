using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

public sealed class Probe : Component
{
    public State<int> Count { get; } = new(0);
    public State<string> Input { get; } = new("initial");
    public string Label { get; set; } = "probe";
    public int Builds, Mounts, Unmounts;
    public bool ChildrenDetachedAtUnmount;
    public override View Body()
    {
        Builds++;
        return VStack(Text($"{Label}: {Count.Value}"), TextField(Input));
    }
    public override void OnMounted() => Mounts++;
    public override void OnUnmounted()
    {
        Unmounts++;
        var before = Builds;
        Count.Value++;
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        ChildrenDetachedAtUnmount = Builds == before;
    }
}
