using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

namespace NativeLifetime;

internal static class MemoryWorkload
{
    internal static async Task<object> Run(int batches, int cycles, int rows, int warmups, bool negative, string reportPath, int focusAtBatch = -1)
    {
        if (focusAtBatch < -1 || focusAtBatch >= warmups + batches) throw new ArgumentOutOfRangeException(nameof(focusAtBatch));
        var traceFocus = Environment.GetEnvironmentVariable("UI_FRAMEWORK_FOCUS_TRACE") == "1";
        if (traceFocus && !MeasurementTools.MemoryCheckpoint.Enabled) throw new InvalidOperationException("Focus tracing requires diagnostic checkpoints.");
        var focusCaptured = false;
        var counters = new LifetimeCounters();
        var samples = new MeasurementTools.BoundedReporting(reportPath + ".samples");
        var window = ProbeUi.Window("Framework sustained native lifetime measurement");
        using var input = new InputObservation(window);
        window.Show(); await ProbeUi.Flush(window);
        for (var batch = 0; batch < warmups + batches; batch++)
        {
            var bytes = GC.GetAllocatedBytesForCurrentThread();
            var timer = Stopwatch.StartNew();
            for (var cycle = 0; cycle < cycles; cycle++) await Cycle(window, counters, rows, negative, batch == focusAtBatch && cycle == 0);
            timer.Stop(); bytes = GC.GetAllocatedBytesForCurrentThread() - bytes;
            var naturalHeap = GC.GetTotalMemory(false);
            await ProbeUi.Collect(window);
            var alive = counters.Alive();
            counters.References.Clear();
            await ProbeUi.Collect(window);
            ProbeUi.Require(counters.Created == counters.Released && counters.NativeBytes == 0
                && counters.BufferCreates == counters.BufferReleases && counters.WrongThreadReleases == 0,
                "Owned native resources remain after a workspace batch.");
            var collectable = negative ? alive.GetValueOrDefault("nativeControl") > 0 : alive.Values.All(count => count == 0);
            if (!collectable) await MeasurementTools.MemoryCheckpoint.Pause("collectability-failure");
            ProbeUi.Require(collectable,
                "Managed collectability invariant failed (or the negative control failed to detect retention): "
                + string.Join(", ", alive.Select(pair => $"{pair.Key}={pair.Value}")));
            ProbeUi.Require(counters.Errors.Count == 0, "Unexpected native initialization failure during memory workload.");
            samples.VerifyPreviousCollected();
            if (batch == warmups)
            {
                if (traceFocus && input.ActivationCount != 0) throw new InvalidOperationException("Focus occurred before the diagnostic baseline.");
                await MeasurementTools.MemoryCheckpoint.Pause("start");
            }
            if (traceFocus && !focusCaptured && input.ActivationCount > 0)
            {
                focusCaptured = true;
                await MeasurementTools.MemoryCheckpoint.Pause("first-activation");
            }
            if (batch == warmups + batches - 1) await MeasurementTools.MemoryCheckpoint.Pause("end");
            Record(samples, batch, batch < warmups, naturalHeap, timer.Elapsed.TotalMilliseconds, bytes, counters, alive, input);
            Console.WriteLine($"Batch {batch}: {counters.Created} native mounts; alive={alive.Values.Sum()}; owned native bytes={counters.NativeBytes}");
        }
        if (MeasurementTools.MemoryCheckpoint.Enabled)
        {
            await Task.Delay(5000);
            await ProbeUi.Collect(window);
            samples.VerifyPreviousCollected();
            await MeasurementTools.MemoryCheckpoint.Pause("quiescent");
        }
        window.Close();
        if (traceFocus && !focusCaptured) throw new InvalidOperationException("No manual focus observed during diagnostic run.");
        var reporting = samples.Complete();
        GC.KeepAlive(counters.Publisher);
        return new { Kind = negative ? "retention-negative-control" : "sustained-native-memory-observation",
            Rows = rows, CyclesPerBatch = cycles, FocusAtBatch = focusAtBatch, WarmupBatches = warmups, MeasuredBatches = batches,
            NegativeControlDetected = negative, Samples = reporting.Samples,
            ReportingRetention = reporting.Measurement, ReleaseGate = false };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Record(MeasurementTools.BoundedReporting samples, int batch, bool warmup, long naturalHeap,
        double milliseconds, long bytes, LifetimeCounters counters, Dictionary<string, int> alive, InputObservation input) =>
        samples.Add(MemoryTelemetry.Read(batch, warmup, naturalHeap, milliseconds, bytes, counters, alive, input.Snapshot()));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task Cycle(Window window, LifetimeCounters counters, int rows, bool negative, bool focus)
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
        if (focus)
        {
            ProbeUi.Require(window.Activate(), "Diagnostic window activation failed.");
            Keyboard.Focus(first);
            ProbeUi.Require(first.IsKeyboardFocused, "Diagnostic editor focus failed.");
        }
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
