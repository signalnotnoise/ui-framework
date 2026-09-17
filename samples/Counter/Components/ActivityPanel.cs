using System.Diagnostics;
using System.Windows.Threading;
using UI_Framework;
using static UI_Framework.UI;

namespace StressLab;

public sealed class ActivityPanel : LabComponent
{
    protected override View Render() => Card(Text("Activity").FontSize(20), VStack(Model.Activity.Reverse().Select(entry => Caption("• " + entry)).ToArray()).Spacing(8));
}
