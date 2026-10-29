using System.Windows.Controls;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

namespace NativeLifetime;

internal static class RetentionWorkload
{
    internal static async Task<object> Run(int rows)
    {
        var counters = new LifetimeCounters();
        var window = ProbeUi.Window("Framework logical row retention checkpoints");
        var ids = Enumerable.Range(0, rows).ToArray();
        using var host = new ViewHost(() => VirtualList(ids.Select(id =>
            Component<MemoryRow>(row => row.Counters = counters).Id(id.ToString())), 280));
        window.Content = host; window.Show(); await ProbeUi.Flush(window);
        var snapshots = new List<object>();
        async Task<Dictionary<string, int>> Capture(string phase)
        {
            await ProbeUi.Collect(window);
            var alive = counters.Alive();
            snapshots.Add(new { Phase = phase, Descriptions = ids.Length, Alive = alive,
                LogicalPayloadBytes = alive.GetValueOrDefault("logicalPayload") * 8192L,
                counters.NativeBytes, Process = MemoryTelemetry.ProcessSnapshot() });
            return alive;
        }
        await Capture("initial-viewport");
        var scroll = ProbeUi.Find<ScrollViewer>(host).First();
        for (var offset = 0d; offset < scroll.ScrollableHeight; offset += Math.Max(1, scroll.ViewportHeight / 2))
        {
            scroll.ScrollToVerticalOffset(offset); await ProbeUi.Flush(window);
        }
        scroll.ScrollToEnd(); await ProbeUi.Flush(window);
        var visited = await Capture("all-rows-visited");
        ProbeUi.Require(visited.GetValueOrDefault("component") == rows, "The retention workload did not visit every logical row.");
        ProbeUi.Require(visited.GetValueOrDefault("nativeControl") < rows / 2, "Native realization was not bounded by the viewport.");
        ids = ids.Take(rows / 2).ToArray(); host.Refresh(); await ProbeUi.Flush(window);
        var half = await Capture("half-keys-removed");
        ProbeUi.Require(half.GetValueOrDefault("component") == ids.Length, "Removed keys retained component snapshots.");
        ids = []; host.Refresh(); await ProbeUi.Flush(window);
        var removed = await Capture("all-keys-removed");
        ProbeUi.Require(removed.Values.All(count => count == 0) && counters.NativeBytes == 0, "Empty list retained visited rows or native resources.");
        host.Dispose(); window.Content = null; await ProbeUi.Flush(window);
        var disposed = await Capture("host-disposed");
        ProbeUi.Require(disposed.Values.All(count => count == 0), "Disposed list retained observed objects.");
        window.Close();
        GC.KeepAlive(counters.Publisher);
        return new { Kind = "logical-row-retention-checkpoints", Rows = rows, Samples = snapshots,
            ReleaseGate = false, WholeApplicationMemoryBudget = false };
    }
}
