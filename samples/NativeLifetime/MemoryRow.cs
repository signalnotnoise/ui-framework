using UI_Framework;
using UI_Framework.Wpf;

namespace NativeLifetime;

internal sealed class MemoryRow : Component
{
    private readonly State<string> text = new("row");
    private readonly byte[] logicalPayload = new byte[8192];
    private bool tracked;
    internal LifetimeCounters Counters { get; set; } = null!;
    public MemoryRow() { }
    public override View Body()
    {
        if (!tracked) { Counters.Track("component", this); Counters.Track("logicalPayload", logicalPayload); tracked = true; }
        var content = text.Value;
        return WpfUI.Native(() => new LifetimeEditor(Counters), input =>
        {
            input.Edited = value => text.Value = value;
            if (input.Text != content) input.Text = content;
        }, input => input.Dispose()).Height(32);
    }
}
