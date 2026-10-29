using UI_Framework;
using static UI_Framework.UI;

public sealed class ShuffleRow : Component
{
    internal State<string> Value = null!;
    internal ShuffleCounters Counters = null!;
    public override View Body() { Counters.Builds++; return TextField(Value).Height(28); }
    public override void OnMounted() => Counters.Mounts++;
    public override void OnUnmounted() => Counters.Unmounts++;
}
