using UI_Framework;
using static UI_Framework.UI;

namespace StressLab;

public sealed class DiagnosticToggle : Component
{
    public string Label { get; set; } = "Toggle";
    public State<bool> Value { get; set; } = new(false);

    public override View Body() => Toggle(Label, Value);
}
