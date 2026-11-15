using UI_Framework;
using static UI_Framework.UI;

public sealed class SkillRowsExample : Component
{
    private readonly StateList<string> items = new(["log-a", "log-b"]);

    public void ReplaceRows(params string[] ids) => items.ReplaceAll(ids);

    public override View Body() => VirtualList(
        items.Select(id => Component<SkillRowExample>(row => row.EntryId = id).Id(id)),
        height: 480);
}
