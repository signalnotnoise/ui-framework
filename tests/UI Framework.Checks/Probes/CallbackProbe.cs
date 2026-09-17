using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;
using System;
public sealed class CallbackProbe : Component
{
    public Action Click { get; set; } = null!;
    public override View Body() => UI_Framework.UI.Button("Click", () => Click());
}
