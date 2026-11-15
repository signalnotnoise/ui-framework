using UI_Framework;
using static UI_Framework.UI;

public sealed class SkillStateExample : Component
{
    private readonly State<string> filter = new("");
    private readonly Computed<string> normalized;

    public SkillStateExample() => normalized = new(() => filter.Value.Trim());

    public void SetFilter(string value) => filter.Value = value;

    public override View Body() => VStack(
        TextField(filter).AccessibilityLabel("Filter"),
        Text(normalized.Value));
}
