using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

namespace NativeLifetime;

internal static class MemoryWorkload
{
    internal static async Task<object> Run(int batches, int cycles, int rows, int warmups, bool negative)
    {
        var counters = new LifetimeCounters();
        var samples = new List<object>();
        var window = ProbeUi.Window("Framework sustained native lifetime measurement");
        window.Show(); await ProbeUi.Flush(window);
        for (var batch = 0; batch < warmups + batches; batch++)
        {
            var bytes = GC.GetAllocatedBytesForCurrentThread();
            var timer = Stopwatch.StartNew();
            for (var cycle = 0; cycle < cycles; cycle++) await Cycle(window, counters, rows, negative);
            timer.Stop(); bytes = GC.GetAllocatedBytesForCurrentThread() - bytes;
            var naturalHeap = GC.GetTotalMemory(false);
            await ProbeUi.Collect(window);
            var alive = counters.Alive();
            counters.References.Clear();
            await ProbeUi.Collect(window);
            ProbeUi.Require(counters.Created == counters.Released && counters.NativeBytes == 0
                && counters.BufferCreates == counters.BufferReleases && counters.WrongThreadReleases == 0,
                "Owned native resources remain after a workspace batch.");
            ProbeUi.Require(negative ? alive.GetValueOrDefault("nativeControl") > 0 : alive.Values.All(count => count == 0),
                "Managed collectability invariant failed (or the negative control failed to detect retention).");
            ProbeUi.Require(counters.Errors.Count == 0, "Unexpected native initialization failure during memory workload.");
            samples.Add(MemoryTelemetry.Read(batch, batch < warmups, naturalHeap, timer.Elapsed.TotalMilliseconds, bytes, counters, alive));
            Console.WriteLine($"Batch {batch}: {counters.Created} native mounts; alive={alive.Values.Sum()}; owned native bytes={counters.NativeBytes}");
        }
        window.Close();
        GC.KeepAlive(counters.Publisher);
        return new { Kind = negative ? "retention-negative-control" : "sustained-native-memory-observation",
            Rows = rows, CyclesPerBatch = cycles, WarmupBatches = warmups, MeasuredBatches = batches,
            NegativeControlDetected = negative, Samples = samples, ReleaseGate = false };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task Cycle(Window window, LifetimeCounters counters, int rows, bool negative)
    {
        var navigation = new NavigationStack<int>(0);
        using var host = new ViewHost(() =>
        {
            _ = counters.Publisher.Value;
            return Navigation(navigation, route => route == 0
                ? VirtualList(Enumerable.Range(0, rows).Select(index => Component<MemoryRow>(row => row.Counters = counters).Id(index.ToString())), 280)
                : Text("detail screen"), NavigationTransition.None);
        });
        counters.Track("host", host);
        window.Content = host; await ProbeUi.Flush(window);
        var first = ProbeUi.Find<LifetimeEditor>(host).First();
        await first.Completion;
        first.Text = "retained local edit";
        if (negative) counters.DeliberateRoots.Add(first);
        var scroll = ProbeUi.Find<ScrollViewer>(host).First();
        foreach (var fraction in new[] { .25, .5, .75, 1.0, 0.0 })
        {
            scroll.ScrollToVerticalOffset(scroll.ScrollableHeight * fraction);
            await ProbeUi.Flush(window);
        }
        navigation.Push(1); await ProbeUi.Flush(window);
        navigation.Back(); await ProbeUi.Flush(window);
        ProbeUi.Require(ProbeUi.Find<LifetimeEditor>(host).Any(input => input.Text == "retained local edit"),
            "Virtualization/navigation failed to preserve component-local edits.");
        counters.Publisher.Value++;
        host.Dispose(); window.Content = null;
        await ProbeUi.Flush(window);
    }
}
