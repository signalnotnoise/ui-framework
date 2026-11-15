using UI_Framework;
using static UI_Framework.UI;

public sealed class SkillRowExample : Component
{
    private readonly State<string> note = new("");
    public string EntryId { get; set; } = "";

    public override View Body() => TextField(note).AccessibilityLabel(EntryId).Height(32);
}
