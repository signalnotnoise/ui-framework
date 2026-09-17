using System.Diagnostics;
using System.Windows.Threading;
using UI_Framework;
using static UI_Framework.UI;

namespace StressLab;

public sealed class TelemetryPanel : LabComponent
{
    private readonly State<string> snapshot = new("Waiting for the first sample…");
    private DispatcherTimer? timer;
    protected override View Render() => Card(Text("Live diagnostics").FontSize(20), Text(snapshot.Value).FontSize(13),
        Caption("Counters include this panel. Memory is total managed heap, not a leak verdict."));
    public override void OnMounted()
    {
        base.OnMounted();
        if (!Model.TelemetryEnabled)
        {
            snapshot.Value = "Live sampling is disabled during automated timing runs.";
            return;
        }
        timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(750) };
        timer.Tick += Sample;
        timer.Start();
    }
    private void Sample(object? sender, EventArgs args)
    {
        using var process = Process.GetCurrentProcess();
        snapshot.Value = $"Body builds: {Model.Counters.Builds:N0}   Active components: {Model.Counters.Active:N0}\n" +
            $"Mounted: {Model.Counters.Mounts:N0}   Unmounted: {Model.Counters.Unmounts:N0}\n" +
            $"Managed memory: {GC.GetTotalMemory(false) / 1048576d:F1} MB   Process: {process.WorkingSet64 / 1048576d:F1} MB";
    }
    public override void OnUnmounted()
    {
        if (timer is not null) { timer.Stop(); timer.Tick -= Sample; timer = null; }
        base.OnUnmounted();
    }
}
