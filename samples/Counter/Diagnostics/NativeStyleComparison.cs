#if NATIVE_ISLAND_BENCHMARK
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

namespace StressLab;

// Supplemental workload: root build counts and native factory/release counts
// are reported separately; neither is a substitute for latency or allocations.
internal static class NativeStyleComparison
{
    internal static int Run(string[] args)
    {
        var reportIndex = Array.IndexOf(args, "--report");
        var path = Path.GetFullPath(args[reportIndex + 1]);
        var preserved = args.Contains("--preserve-native");
        var initialScope = preserved ? NativeStyleScope.PreserveNative : NativeStyleScope.Inherit;
        var phase = new State<int>(0);
        var panels = new StackPanel?[32];
        var inputs = new TextBox?[32];
        var buttons = new Button?[32];
        var creates = 0;
        var releases = 0;
        var builds = 0;
        var local = new Style(typeof(Button));
        local.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.ForestGreen));
        var explicitStyle = new Style(typeof(TextBox));
        explicitStyle.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.Blue));
        StackPanel Create(int index)
        {
            creates++;
            var panel = new StackPanel();
            if (index % 4 == 0) panel.Resources[typeof(Button)] = local;
            var input = new TextBox { Text = $"Search island {index}", Width = 180 };
            if (index % 4 == 0) input.Style = explicitStyle;
            var button = new Button { Content = "Find next" };
            var tree = new TreeView { Height = 80 };
            for (var item = 0; item < 4; item++)
                tree.Items.Add(new TreeViewItem { Header = $"File {index}/{item}.cs" });
            panel.Children.Add(input);
            panel.Children.Add(button);
            panel.Children.Add(new CheckBox { Content = "Match case" });
            panel.Children.Add(tree);
            panels[index] = panel; inputs[index] = input; buttons[index] = button;
            input.Select(2, 3);
            return panel;
        }
        View Build()
        {
            builds++;
            var step = phase.Value;
            var scope = step is >= 21 and <= 30
                ? (initialScope == NativeStyleScope.Inherit ? NativeStyleScope.PreserveNative : NativeStyleScope.Inherit)
                : initialScope;
            return Scroll(AdaptiveGrid(240, Enumerable.Range(0, 32)
                .Where(index => step < 41 || step % 2 == 0 || index % 2 == 0)
                .Select(index => WpfUI.Native(() => Create(index), scope,
                    update: panel => ((Button)panel.Children[1]).Content = $"Find next {step}",
                    release: panel =>
                    {
                        Require(panel.Parent is null, "Release must follow detachment.");
                        releases++;
                    }).Id($"island-{index}")).ToArray())).Height(1080);
        }
        using var source = new HwndSource(new HwndSourceParameters("Native island benchmark")
        {
            Width = 1320, Height = 1080, PositionX = -10000, PositionY = -10000,
            WindowStyle = unchecked((int)0x80000000)
        });
        var mountAllocation = GC.GetAllocatedBytesForCurrentThread();
        var watch = Stopwatch.StartNew();
        using var host = new ViewHost(Build);
        source.RootVisual = host;
        ThemeStyles.Apply(host, new ThemeTokens());
        Layout(host);
        watch.Stop();
        var mountTime = watch.Elapsed.TotalMilliseconds;
        var mountBytes = GC.GetAllocatedBytesForCurrentThread() - mountAllocation;
        var mountBuilds = builds;
        var initialCreates = creates;
        var initialInputs = inputs.ToArray();
        var phases = new List<object>();
        var totalTime = 0d;
        long totalBytes = 0;
        for (var batch = 0; batch < 5; batch++)
        {
            var allocation = GC.GetAllocatedBytesForCurrentThread();
            watch.Restart();
            for (var step = batch * 10 + 1; step <= (batch + 1) * 10; step++)
            {
                phase.Value = step;
                if (batch == 1) ThemeStyles.Apply(host, new ThemeTokens
                { Ink = step % 2 == 0 ? "#101010" : "#FFFFFF", Surface = step % 2 == 0 ? "#FFFFFF" : "#181818" });
                Layout(host);
            }
            watch.Stop();
            var bytes = GC.GetAllocatedBytesForCurrentThread() - allocation;
            totalTime += watch.Elapsed.TotalMilliseconds; totalBytes += bytes;
            phases.Add(new { Name = new[] { "retained-updates", "outer-theme-replacement", "scope-switch", "scope-restore", "remove-remount" }[batch],
                Milliseconds = watch.Elapsed.TotalMilliseconds, AllocatedBytes = bytes });
            // Validate outside the measured intervals, including all retained rows.
            for (var index = 0; index < 32; index++)
            {
                var input = inputs[index]!;
                if (batch < 4 || index % 2 == 0)
                    Require(ReferenceEquals(initialInputs[index], input), "Retained update replaced an input.");
                Require(input.Text == $"Search island {index}" && input.SelectionStart == 2 && input.SelectionLength == 3,
                    "Native edit/selection changed.");
                if (index % 4 == 0)
                    Require(ReferenceEquals(local, buttons[index]!.Style) && ReferenceEquals(explicitStyle, input.Style),
                        "Local style precedence changed.");
            }
        }
        Require(creates == 112 && releases == 80, "Unexpected native lifecycle work.");
        var updateCreates = creates - initialCreates;
        var updateReleases = releases;
        source.RootVisual = null;
        host.Dispose();
        Require(creates == releases && panels.All(panel => panel!.Parent is null), "Native lifetime imbalance.");
        var result = new
        {
            SchemaVersion = 2, Scenario = preserved ? "native-preserve" : "native-inherit",
            Rows = 32, MixedOperations = 50, Presentation = "hidden-hwnd", InitialScope = initialScope.ToString(),
            MountMilliseconds = mountTime, MountAllocatedBytes = mountBytes, MountBodyBuilds = mountBuilds,
            Milliseconds = totalTime, AllocatedBytes = totalBytes, BodyBuilds = builds - mountBuilds,
            Mounts = updateCreates, Unmounts = updateReleases, TotalCreates = creates, TotalReleases = releases,
            Phases = phases, Runtime = Environment.Version.ToString(), TimeUtc = DateTimeOffset.UtcNow
        };
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine(JsonSerializer.Serialize(result));
        return 0;
    }

    private static void Layout(FrameworkElement host)
    {
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        host.Measure(new Size(1320, 1080));
        host.Arrange(new Rect(0, 0, 1320, 1080));
        host.UpdateLayout();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
#endif
