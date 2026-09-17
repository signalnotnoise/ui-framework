using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

public sealed class OtherProbe : Component
{
    public int Mounts;
    public override View Body() => Text("Replacement");
    public override void OnMounted() => Mounts++;
}
