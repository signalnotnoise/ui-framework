using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

public sealed class MountMutation : Component
{
    public State<int> Count { get; } = new(0);
    public int Builds;
    public override View Body() { Builds++; return Text(Count.Value.ToString()); }
    public override void OnMounted() => Count.Value++;
}
