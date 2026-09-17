using UI_Framework;
using static UI_Framework.UI;

public sealed class VirtualizationProbe : Component
{
    public int Id { get; set; }
    public State<string> Text { get; } = new("initial");
    public State<bool> Expanded { get; } = new(false);
    public int Builds, Mounts, Unmounts;
    public override View Body()
    {
        Builds++;
        return TextField(Text).Height(Expanded.Value ? 160 : 40);
    }
    public override void OnMounted() => Mounts++;
    public override void OnUnmounted() => Unmounts++;
}
