using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

public sealed class MemoProbe : Component
{
    public State<int> Local { get; } = new(0);
    public int Prop { get; set; }
    public int Builds;
    public override View Body() { Builds++; return Text($"{Prop}: {Local.Value}"); }
}
