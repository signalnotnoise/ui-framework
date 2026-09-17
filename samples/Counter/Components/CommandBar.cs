using System.Diagnostics;
using System.Windows.Threading;
using UI_Framework;
using static UI_Framework.UI;

namespace StressLab;

public sealed class CommandBar : LabComponent
{
    protected override View Render() => Card(
        HStack(Button("50 rows", () => Model.Load(50)), Button("250 rows", () => Model.Load(250)), Button("1,000 rows", () => Model.Load(1000)),
            Button("+ Work item", Model.Add), Button("Shuffle", Model.Shuffle), Button("10,000 writes", () => Model.Burst()),
            Button("Run 200 steps", Model.Start), Button("▶ Visual stress", Model.StartVisualStress), Button("Stop", Model.Stop)).Spacing(8),
        Text(Model.RunStatus.Value).FontSize(13)
    );
}
