using System.Diagnostics;
using System.Windows.Threading;
using UI_Framework;
using static UI_Framework.UI;

namespace StressLab;

public sealed class DepthPanel : LabComponent
{
    protected override View Render() => Card(
        Text("Nested component tree").FontSize(20),
        HStack(Button("−", () => Model.Depth.Value = Math.Max(1, Model.Depth.Value - 1)), Text($"Depth {Model.Depth.Value}"), Button("+", () => Model.Depth.Value = Math.Min(24, Model.Depth.Value + 1))).Spacing(12),
        Component<DepthNode>(node => { node.Model = Model; node.Remaining = Model.Depth.Value; }).Id("root").Memo((Model, Model.Depth.Value))
    );
}
