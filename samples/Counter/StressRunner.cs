using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using UI_Framework.Wpf;
using static UI_Framework.UI;

namespace StressLab;

internal static class StressRunner
{
    public static int Compare(string[] args)
    {
        var path = Path.GetFullPath(Option(args, "--report", "artifacts/stress/comparison.json"));
        using var model = new WorkspaceModel(1000) { TelemetryEnabled = false };
        model.Virtualized.Value = args.Contains("--virtualized");
        var mountAllocation = GC.GetAllocatedBytesForCurrentThread();
        var mountWatch = Stopwatch.StartNew();
        using var host = new ViewHost(() => Component<Dashboard>(screen => screen.Model = model));
        if (args.Contains("--themed")) ThemeStyles.Apply(host, new UI_Framework.ThemeTokens());
        Flush();
        host.Measure(new Size(1320, 1080));
        host.Arrange(new Rect(0, 0, 1320, 1080));
        host.UpdateLayout();
        mountWatch.Stop();
        var mountBytes = GC.GetAllocatedBytesForCurrentThread() - mountAllocation;
        var mountBuilds = model.Counters.Builds;
        Console.WriteLine("Comparison tree mounted: 1,000 rows. Measuring 50 mixed operations.");
        var before = model.Counters.Builds;
        var beforeMounts = model.Counters.Mounts;
        var beforeUnmounts = model.Counters.Unmounts;
        var allocations = GC.GetAllocatedBytesForCurrentThread();
        var watch = Stopwatch.StartNew();
        for (var step = 0; step < 50; step++)
        {
            model.MixedStep(step);
            Flush();
            host.UpdateLayout();
        }
        watch.Stop();
        var result = new
        {
            SchemaVersion = 2,
            Scenario = args.Contains("--themed") ? "themed-full-list" : model.Virtualized.Value ? "virtualized" : "full-list",
            MountMilliseconds = mountWatch.Elapsed.TotalMilliseconds, MountAllocatedBytes = mountBytes, MountBodyBuilds = mountBuilds,
            Mode = args.Contains("--reference") ? "unfiltered observation, memo disabled" : "selective observation, memo enabled",
            Rows = 1000, MixedOperations = 50, Milliseconds = watch.Elapsed.TotalMilliseconds,
            BodyBuilds = model.Counters.Builds - before,
            AllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocations,
            Mounts = model.Counters.Mounts - beforeMounts, Unmounts = model.Counters.Unmounts - beforeUnmounts,
            Runtime = Environment.Version.ToString(),
            TimeUtc = DateTimeOffset.UtcNow
        };
        Require(model.Items.Count == 1000, "Comparison changed the expected row count.");
        host.Dispose();
        Require(model.Counters.Active == 0, "Comparison leaked mounted components.");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine(JsonSerializer.Serialize(result));
        return 0;
    }


    private static void Flush() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static string Option(string[] args, string key, string fallback)
    {
        var index = Array.IndexOf(args, key);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback;
    }

    public static int Run(string[] args)
    {
        var reportPath = Path.GetFullPath(Option(args, "--report", "artifacts/stress/latest.json"));
        var snapshotPath = Path.GetFullPath(Option(args, "--snapshot", "artifacts/stress/dashboard.png"));
        List<Measurement> measurements = [];
        var assertions = 0;
        void Check(bool condition, string message) { Require(condition, message); assertions++; }
        using var model = new WorkspaceModel(50) { TelemetryEnabled = false };
        ViewHost? host = null;
        void Measure(string name, Action operation, bool layout = true)
        {
            var builds = model.Counters.Builds;
            var mounts = model.Counters.Mounts;
            var unmounts = model.Counters.Unmounts;
            var allocation = GC.GetAllocatedBytesForCurrentThread();
            var watch = Stopwatch.StartNew();
            operation();
            Flush();
            if (layout && host is not null)
            {
                host.Measure(new Size(1320, 1080));
                host.Arrange(new Rect(0, 0, 1320, 1080));
                host.UpdateLayout();
            }
            watch.Stop();
            measurements.Add(new(name, watch.Elapsed.TotalMilliseconds, model.Counters.Builds - builds,
                model.Counters.Mounts - mounts, model.Counters.Unmounts - unmounts,
                GC.GetAllocatedBytesForCurrentThread() - allocation));
            Console.WriteLine($"{name}: {watch.Elapsed.TotalMilliseconds:F1} ms, {model.Counters.Builds - builds:N0} body builds");
        }
        try
        {
            Measure("Initial 50-row mount + offscreen layout", () => host = new ViewHost(() => Component<Dashboard>(screen => screen.Model = model)));
            Check(model.Counters.Active > 50, "Initial component tree did not mount.");

            // Exercise actual TextBox events, not only model setters. The shared editor must mirror the row.
            var selected = model.Selected.Value!;
            var editor = Descendants<TextBox>(host!).First(input => input.Text == selected.Title.Value);
            Measure("Native TextBox event + shared binding", () => editor.Text = "Edited through native TextBox");
            Check(selected.Title.Value == "Edited through native TextBox", "Native text edit did not reach state.");
            Check(Descendants<TextBox>(host!).Count(input => input.Text == selected.Title.Value) >= 2, "Shared row and inspector did not synchronize.");
            var completed = Descendants<CheckBox>(host!).First(toggle => Equals(toggle.Content, "Completed"));
            completed.IsChecked = true;
            Flush();
            Check(selected.Done.Value && Descendants<CheckBox>(host!).Any(toggle => Equals(toggle.Content, "Done") && toggle.IsChecked == true), "Native toggle did not update shared binding.");

            Measure("Load 250 rows + layout", () => model.Load(250));
            Measure("Load 1000 rows + layout", () => model.Load(1000));
            Check(model.Items.Count == 1000 && model.Counters.Active >= 1000, "Large list was not fully mounted.");
            var first = model.Items[0];
            var firstInput = Descendants<TextBox>(host!).First(input => input.Text == first.Title.Value);
            var expand = Descendants<Button>(host!).First(button => Equals(button.Content, "More"));
            Measure("Expand row into nested checklist + layout", () => expand.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent)));
            Check(Equals(expand.Content, "Less"), "Row did not expand through its native button event.");
            first.Checklist[0].Title.Value = "Shared nested checklist edit";
            Flush();
            Check(Descendants<TextBox>(host!).Count(input => input.Text == first.Checklist[0].Title.Value) == 2, "Row and inspector checklist bindings did not synchronize.");
            Measure("1000-row keyed shuffle + layout", model.Shuffle);
            Check(Descendants<TextBox>(host!).Any(input => ReferenceEquals(input, firstInput)), "Shuffle replaced an existing input control.");
            Check(model.Items.Any(item => ReferenceEquals(item, first)), "Shuffle replaced the data model.");
            Check(Equals(expand.Content, "Less") && Descendants<Button>(host!).Any(button => ReferenceEquals(button, expand)), "Shuffle lost row-local expansion state.");

            var beforeBurst = model.Counters.Builds;
            Measure("10000 projected writes + one flush + layout", () => model.Burst());
            Check(model.Counters.Builds - beforeBurst < 100, "A burst produced unbatched component work.");
            Check(model.Items[0].Notes.Value == $"Burst mutation {10000:N0}", "Projected burst lost the final value.");

            Measure("200 mixed operations with per-step render + layout", () =>
            {
                for (var step = 0; step < 200; step++)
                {
                    model.MixedStep(step);
                    Flush();
                    host!.UpdateLayout();
                    if ((step + 1) % 50 == 0) Console.WriteLine($"Mixed workload: {step + 1}/200 operations complete");
                }
            });
            Check(model.Items.Count == 1000, "Mixed add/remove campaign changed expected row count.");
            Check(model.Items.Select(item => item.Id).Distinct().Count() == model.Items.Count, "Duplicate row identity after campaign.");
            Check(model.Selected.Value is null || model.Items.Contains(model.Selected.Value), "Selection references a removed item.");

            Measure("Filter to zero rows + layout", () => model.Query.Value = "__no_matches__");
            Check(Descendants<TextBox>(host!).Count() < 40, "Filtered rows remained mounted.");
            Measure("Clear filter + remount + layout", () => { model.Query.Value = ""; model.OnlyOpen.Value = false; });

            var active = model.Counters.Active;
            Measure("Board unmount + layout", () => model.BoardMounted.Value = false);
            Check(active - model.Counters.Active >= 1000, "Board removal did not unmount row components.");
            Measure("Board remount + layout", () => model.BoardMounted.Value = true);
            Check(model.Counters.Active == active, "Board remount changed expected active component count.");
            Measure("Expand recursive tree to depth 24 + layout", () => model.Depth.Value = 24);
            Check(Descendants<TextBlock>(host!).Count(text => text.Text.StartsWith("Layer ", StringComparison.Ordinal)) == 23, "Recursive component tree did not reach the requested depth.");

            // Capture the actual app at a readable size after the stress workload.
            model.Load(50);
            model.Depth.Value = 4;
            model.CompactRows.Value = false;
            model.RunStatus.Value = "Automated stress run passed. Ready for manual input, scrolling, and focus checks.";
            Flush();
            host!.Measure(new Size(1320, 1080));
            host.Arrange(new Rect(0, 0, 1320, 1080));
            host.UpdateLayout();
            Directory.CreateDirectory(Path.GetDirectoryName(snapshotPath)!);
            var bitmap = new RenderTargetBitmap(1320, 1080, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(host);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (var stream = File.Create(snapshotPath)) encoder.Save(stream);

            Measure("Full host disposal", host.Dispose, layout: false);
            Check(model.Counters.Active == 0, "Components survived root host disposal.");
            var afterDispose = model.Counters.Builds;
            model.Items[0].Title.Value = "After disposal";
            Flush();
            Check(model.Counters.Builds == afterDispose, "Disposed components still respond to state.");
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
            File.WriteAllText(reportPath, JsonSerializer.Serialize(new
            {
                Passed = true, Assertions = assertions, Rows = 1000, BurstWrites = 10000, MixedOperations = 200,
                Mode = "WPF controls + dispatcher + offscreen layout; not a visible-window FPS or keyboard-focus benchmark",
                TimeUtc = DateTimeOffset.UtcNow, Runtime = Environment.Version.ToString(),
                TotalMounts = model.Counters.Mounts, TotalUnmounts = model.Counters.Unmounts,
                Measurements = measurements
            }, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"Passed {assertions} stress assertions. Report: {reportPath}");
            return 0;
        }
        catch (Exception error)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
            File.WriteAllText(reportPath, JsonSerializer.Serialize(new { Passed = false, Assertions = assertions, Error = error.ToString(), Measurements = measurements }, new JsonSerializerOptions { WriteIndented = true }));
            Console.Error.WriteLine(error);
            return 1;
        }
        finally { host?.Dispose(); }
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T match) yield return match;
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
            foreach (var nested in Descendants<T>(child)) yield return nested;
    }
}
