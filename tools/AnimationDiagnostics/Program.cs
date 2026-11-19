using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

namespace AnimationDiagnostics;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        var enabled = args.Contains("--enabled");
        var report = args[Array.IndexOf(args, "--report") + 1];
        const int nodes = 32, operations = 240, warmup = 30;
        var spec = new AnimationSpec(TimeSpan.FromMilliseconds(160));
        var turn = 0;
        var builds = 0;
        var measured = false;
        var refresh = new double[operations];
        var gaps = new List<double>(operations * 2);
        var frameCount = 0;
        var ticks = 0;
        long allocationStart = 0, refreshAllocations = 0, lastFrame = 0, start = 0;
        var initialBuilds = 0;
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        var window = new Window { Width = 640, Height = 480, Left = -10000, Top = -10000,
            ShowInTaskbar = false, ShowActivated = false, Title = "Animation diagnostics" };
        new WindowInteropHelper(window).EnsureHandle();
        var mountAllocated = GC.GetAllocatedBytesForCurrentThread();
        var mountStarted = Stopwatch.GetTimestamp();
        using var host = new ViewHost(() =>
        {
            builds++;
            return VStack(Enumerable.Range(0, nodes).Select(i => Text($"Item {i}").Id(i.ToString())).ToArray());
        });
        var mountMs = Stopwatch.GetElapsedTime(mountStarted).TotalMilliseconds;
        var mountBytes = GC.GetAllocatedBytesForCurrentThread() - mountAllocated;
        AnimationSettings.SetIsEnabled(host, enabled);
        window.Content = host;
        // Isolate native-clock cost without changing the user's Windows preference.
        // Public policy enforcement is covered separately by AnimationTests.
        window.Show();
        window.UpdateLayout();
        var frames = Descendants(host).OfType<Border>().Where(border => border.Child is TextBlock).ToArray();
        if (frames.Length != nodes) throw new InvalidOperationException("Unexpected workload tree.");
        var setupStart = Stopwatch.GetTimestamp();
        var setupAllocated = GC.GetAllocatedBytesForCurrentThread();
        var controllers = frames.Select(frame => new NodeAnimation(frame)).ToArray();
        var setupMs = Stopwatch.GetElapsedTime(setupStart).TotalMilliseconds;
        var setupBytes = GC.GetAllocatedBytesForCurrentThread() - setupAllocated;
        ViewMotion? previous = null;
        var timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(8) };
        void Render(object? sender, EventArgs e)
        {
            if (!measured) return;
            var now = Stopwatch.GetTimestamp();
            if (lastFrame != 0) gaps.Add(Stopwatch.GetElapsedTime(lastFrame, now).TotalMilliseconds);
            lastFrame = now;
            frameCount++;
        }
        CompositionTarget.Rendering += Render;
        timer.Tick += (_, _) =>
        {
            if (ticks == warmup)
            {
                GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
                initialBuilds = builds;
                allocationStart = GC.GetAllocatedBytesForCurrentThread();
                start = Stopwatch.GetTimestamp();
                measured = true;
            }
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            var timestamp = Stopwatch.GetTimestamp();
            turn++;
            host.Refresh();
            var next = new ViewMotion(turn % 2 == 0 ? 1 : .3, turn % 2 == 0 ? 0 : 12, turn % 2 == 0 ? 0 : -6,
                AnimationProperties.Opacity | AnimationProperties.Translation, spec);
            foreach (var controller in controllers) controller.Update(previous, next, enabled);
            previous = next;
            var elapsed = Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds;
            var bytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
            if (measured)
            {
                refresh[ticks - warmup] = elapsed;
                refreshAllocations += bytes;
            }
            ticks++;
            if (ticks != warmup + operations) return;
            var totalBytes = GC.GetAllocatedBytesForCurrentThread() - allocationStart;
            var wallMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            measured = false;
            timer.Stop();
            CompositionTarget.Rendering -= Render;
            var animatedFrames = Descendants(host).OfType<Border>().Count(border => border.HasAnimatedProperties);
            if (enabled && animatedFrames != nodes) throw new InvalidOperationException("Expected one active opacity clock per target.");
            if (!enabled && animatedFrames != 0) throw new InvalidOperationException("Disabled motion created clocks.");
            foreach (var controller in controllers) controller.Dispose();
            host.Dispose();
            window.Close();
            File.WriteAllText(report, JsonSerializer.Serialize(new
            {
                Protocol = "animation-retarget-v1", Enabled = enabled, Nodes = nodes, Operations = operations,
                Runtime = Environment.Version.ToString(), OS = Environment.OSVersion.ToString(),
                WindowsMotionEnabled = SystemParameters.ClientAreaAnimation,
                Policy = "isolated-controller-native-clocks-versus-immediate; OS preference unchanged",
                DurationMs = 160, RequestedIntervalMs = 8, MountMs = mountMs, MountBytes = mountBytes,
                ControllerSetupMs = setupMs, ControllerSetupBytes = setupBytes,
                RefreshTotalMs = refresh.Sum(), RefreshMedianMs = Percentile(refresh, .5), RefreshP95Ms = Percentile(refresh, .95),
                RefreshAllocatedBytes = refreshAllocations, TotalUiThreadAllocatedBytes = totalBytes,
                BodyBuilds = builds - initialBuilds, ComponentBuilds = 0, WallMs = wallMs,
                RenderingCallbacks = frameCount, RenderingGapMedianMs = Percentile(gaps.ToArray(), .5),
                RenderingGapP95Ms = Percentile(gaps.ToArray(), .95), AnimatedFramesAtLastUpdate = animatedFrames,
                RefreshSamplesMs = refresh, RenderingGapsMs = gaps,
                Note = "Rendering callback intervals are not presented-frame FPS. Mounted offscreen HWND; 32 text nodes, no component bodies."
            }, new JsonSerializerOptions { WriteIndented = true }));
            app.Shutdown();
        };
        timer.Start();
        app.Run();
        return 0;
    }

    private static double Percentile(double[] values, double percentile)
    {
        if (values.Length == 0) return 0;
        var sorted = values.Order().ToArray();
        return sorted[(int)Math.Ceiling((sorted.Length - 1) * percentile)];
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        yield return root;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            foreach (var child in Descendants(VisualTreeHelper.GetChild(root, i))) yield return child;
    }
}
