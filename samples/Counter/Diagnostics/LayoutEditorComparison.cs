using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Interop;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

namespace StressLab;

internal static class LayoutEditorComparison
{
    internal static int Run(Application application, string[] args)
    {
        application.Dispatcher.InvokeAsync(async () =>
        {
            try { application.Shutdown(await Measure(args)); }
            catch (Exception error) { Console.Error.WriteLine(error); application.Shutdown(1); }
        });
        return application.Run();
    }

    private static async Task<int> Measure(string[] args)
    {
        var reportIndex = Array.IndexOf(args, "--report");
        var path = Path.GetFullPath(reportIndex >= 0 ? args[reportIndex + 1] : "artifacts/layout-editors.json");
        var phase = new State<int>(0);
        var builds = 0;
        View Build()
        {
            builds++;
            var step = phase.Value;
            return Scroll(AdaptiveGrid(240, Enumerable.Range(0, 1000).Select(index =>
                new View(ViewKind.TextEditor)
                {
                    Content = $"Editor {index}: revision {step / 5}",
                    ReadOnly = index % 5 == 0 && step % 2 == 1,
                    MaximumLength = 2000,
                    UndoHistoryLimit = 20,
                    DesiredHeight = 60
                }.Id(index.ToString())).ToArray()).Spacing(8)).Height(1080);
        }
        var mountAllocation = GC.GetAllocatedBytesForCurrentThread();
        var watch = Stopwatch.StartNew();
        using var host = new ViewHost(Build);
        using var source = new HwndSource(new HwndSourceParameters("Layout/editor benchmark")
        {
            Width = 1320, Height = 1080, PositionX = -10000, PositionY = -10000,
            WindowStyle = unchecked((int)0x80000000)
        });
        source.RootVisual = host;
        ThemeStyles.Apply(host, new ThemeTokens());
        async Task Layout(int step)
        {
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            var width = step % 2 == 0 ? 1320 : 1040;
            host.Measure(new Size(width, 1080));
            host.Arrange(new Rect(0, 0, width, 1080));
            host.UpdateLayout();
        }
        await Layout(0);
        watch.Stop();
        var mountMilliseconds = watch.Elapsed.TotalMilliseconds;
        var mountBytes = GC.GetAllocatedBytesForCurrentThread() - mountAllocation;
        var mountBuilds = builds;
        var allocation = GC.GetAllocatedBytesForCurrentThread();
        watch.Restart();
        for (var step = 1; step <= 50; step++) { phase.Value = step; await Layout(step); }
        watch.Stop();
        var result = new
        {
            SchemaVersion = 2, Scenario = "layout-editors", Rows = 1000, MixedOperations = 50,
            MountMilliseconds = mountMilliseconds, MountAllocatedBytes = mountBytes, MountBodyBuilds = mountBuilds,
            Milliseconds = watch.Elapsed.TotalMilliseconds, AllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocation,
            BodyBuilds = builds - mountBuilds, Mounts = 0, Unmounts = 0,
            Runtime = Environment.Version.ToString(), TimeUtc = DateTimeOffset.UtcNow
        };
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }
}
