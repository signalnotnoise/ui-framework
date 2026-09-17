using System.Diagnostics;
using System.Windows.Threading;
using UI_Framework;
using static UI_Framework.UI;

namespace StressLab;

public sealed class DepthNode : LabComponent
{
    public int Remaining { get; set; }
    private readonly State<int> value = new(0);
    protected override View Render() => Remaining <= 1
        ? HStack(Text($"Leaf state: {value.Value}"), Button("Increment leaf", () => value.Value++)).Spacing(8)
        : VStack(Caption($"Layer {Remaining}"), Component<DepthNode>(node => { node.Model = Model; node.Remaining = Remaining - 1; }).Id("child").Memo((Model, Remaining - 1))).Spacing(3).Padding(3);
}
