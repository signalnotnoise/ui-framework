using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        var app = new Application();
        app.Dispatcher.InvokeAsync(async () =>
        {
            try { await Measure(args); app.Shutdown(0); }
            catch (Exception error) { Console.Error.WriteLine(error); app.Shutdown(1); }
        });
        return app.Run();
    }

    private static async Task Measure(string[] args)
    {
        var native = args.Contains("--native");
        var listIndex = Array.IndexOf(args, "--submission-list");
        var listMode = listIndex < 0 ? "legacy" : args[listIndex + 1];
        if (listMode is not ("legacy" or "full" or "virtualized")) throw new ArgumentException("Unknown list mode.");
        LayoutProbe.Enabled = args.Contains("--probe-layout");
        var isolateIndex = Array.IndexOf(args, "--isolate");
        var isolate = isolateIndex < 0 ? "workspace" : args[isolateIndex + 1];
        if (isolate is not ("workspace" or "dock" or "column" or "split"))
            throw new ArgumentException("Unknown isolation scenario.");
        var report = args[Array.IndexOf(args, "--report") + 1];
        var size = new State<double>(260);
        var collapsed = new State<bool>(false);
        var builds = 0;
        var nativeCreates = 0;
        var nativeReleases = 0;
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        var watch = Stopwatch.StartNew();
        SubmissionListScenario? submissionList = null;
        FrameworkElement submissions;
        if (listMode == "legacy")
        {
            var rows = new StackPanel();
            for (var i = 0; i < 1000; i++) rows.Children.Add(new TextBlock { Text = $"Submission {i}", Height = 24 });
            submissions = new ScrollViewer { Content = rows, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        }
        else submissions = (submissionList = new SubmissionListScenario(listMode == "virtualized")).Control;
        var editor = new TextBox { Text = string.Join('\n', Enumerable.Range(0, 1000).Select(i => $"Line {i}: retained document")),
            AcceptsReturn = true, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var hosts = new List<ViewHost>();
        var editorProbe = new LayoutProbe { Child = editor };
        var submissionsProbe = new LayoutProbe { Child = submissions };
        ViewHost Host(Func<View> body)
        {
            var result = new ViewHost(() => { builds++; return body(); });
            hosts.Add(result);
            return result;
        }
        View Native(FrameworkElement element, string key) => WpfUI.Native(() => { nativeCreates++; return element; },
            release: _ => nativeReleases++).Id(key);
        static DockPanel FillBelow(FrameworkElement header, FrameworkElement body)
        {
            var panel = new DockPanel(); DockPanel.SetDock(header, System.Windows.Controls.Dock.Top);
            panel.Children.Add(header); panel.Children.Add(body); return panel;
        }
        Grid? nativePanes = null;
        FrameworkElement? nativeFirst = null;
        FrameworkElement? nativeSeparator = null;
        ViewHost root;
        if (native || isolate != "workspace")
        {
            FrameworkElement Pane(string title, FrameworkElement content, string key) => !native && isolate == "column"
                ? Host(() => FlexColumn(Text(title).Height(28), Native(content, key)))
                : FillBelow(Host(() => Text(title).Height(28)), Host(() => Native(content, key)));
            nativeFirst = Pane("Submissions", submissionsProbe, "submissions");
            var second = Pane("Document", editorProbe, "editor");
            nativePanes = new Grid { ClipToBounds = true };
            nativePanes.ColumnDefinitions.Add(new() { Width = new(260), MinWidth = 160 });
            nativePanes.ColumnDefinitions.Add(new() { Width = new(6) });
            nativePanes.ColumnDefinitions.Add(new() { Width = new(1, GridUnitType.Star), MinWidth = 160 });
            nativeSeparator = new GridSplitter { Width = 6, HorizontalAlignment = HorizontalAlignment.Stretch,
                ResizeDirection = GridResizeDirection.Columns, ResizeBehavior = GridResizeBehavior.PreviousAndNext };
            nativePanes.Children.Add(nativeFirst);
            Grid.SetColumn(nativeSeparator, 1); nativePanes.Children.Add(nativeSeparator);
            Grid.SetColumn(second, 2); nativePanes.Children.Add(second);
            FrameworkElement panes = nativePanes;
            if (!native && isolate == "split")
            {
                nativePanes.Children.Clear();
                panes = Host(() => SplitPane(WpfUI.Native(() => nativeFirst), WpfUI.Native(() => second),
                    size, minimumFirst: 160, minimumSecond: 160, firstCollapsed: collapsed.Value));
                nativePanes = null;
            }
            var shell = new DockPanel();
            var header = Host(() => Text("Assignment review").Height(50));
            var footer = Host(() => Text("Ready").Height(30));
            DockPanel.SetDock(header, System.Windows.Controls.Dock.Top); shell.Children.Add(header);
            DockPanel.SetDock(footer, System.Windows.Controls.Dock.Bottom); shell.Children.Add(footer);
            shell.Children.Add(panes);
            if (!native && isolate == "dock")
            {
                shell.Children.Clear();
                root = Host(() => Dock(WpfUI.Native(() => panes), top: WpfUI.Native(() => header).Height(50),
                    bottom: WpfUI.Native(() => footer).Height(30)));
            }
            else root = Host(() => WpfUI.Native(() => shell));
        }
        else
        {
            root = Host(() => Dock(
                SplitPane(FlexColumn(Text("Submissions").Height(28), Native(submissionsProbe, "submissions")),
                    FlexColumn(Text("Document").Height(28), Native(editorProbe, "editor")),
                    size, minimumFirst: 160, minimumSecond: 160, firstCollapsed: collapsed.Value),
                top: Text("Assignment review").Height(50), bottom: Text("Ready").Height(30)));
        }
        using var source = new HwndSource(new HwndSourceParameters("Workspace adapter comparison")
        { Width = 1320, Height = 900, PositionX = -10000, PositionY = -10000, WindowStyle = unchecked((int)0x80000000) });
        source.RootVisual = root;
        ThemeStyles.Apply(root, new ThemeTokens());
        async Task Layout(int step)
        {
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            var width = step % 2 == 0 ? 1320 : 1040;
            root.Measure(new Size(width, 900)); root.Arrange(new Rect(0, 0, width, 900)); root.UpdateLayout();
            var expected = collapsed.Value ? width : width - size.Value - 6;
            var origin = editor.TranslatePoint(new Point(), root);
            var expectedX = collapsed.Value ? 0 : size.Value + 6;
            if (Math.Abs(editor.ActualWidth - expected) > 0.1 || Math.Abs(editor.ActualHeight - 792) > 0.1)
                throw new InvalidOperationException($"Unequal workload geometry at {step}: editor {editor.ActualWidth}x{editor.ActualHeight}, expected {expected}x792.");
            if (Math.Abs(origin.X - expectedX) > 0.1 || Math.Abs(origin.Y - 78) > 0.1)
                throw new InvalidOperationException($"Unequal editor position at {step}: {origin}; expected {expectedX},78.");
        }
        await Layout(0);
        watch.Stop();
        var mountMs = watch.Elapsed.TotalMilliseconds;
        var mountBytes = GC.GetAllocatedBytesForCurrentThread() - bytes;
        var mountBuilds = builds;
        var mountRealizedRows = submissionList?.RealizedCount ?? 1000;
        editor.Select(3, 7);
        var mountProbes = new { Editor = editorProbe.Snapshot(), Submissions = submissionsProbe.Snapshot() };
        editorProbe.Reset(); submissionsProbe.Reset();
        bytes = GC.GetAllocatedBytesForCurrentThread(); watch.Restart();
        for (var step = 1; step <= 50; step++)
        {
            size.Value = 260 + step % 4 * 10;
            collapsed.Value = step % 3 == 0;
            if (nativePanes is not null)
            {
                nativePanes.ColumnDefinitions[0].MinWidth = collapsed.Value ? 0 : 160;
                nativePanes.ColumnDefinitions[0].Width = new(collapsed.Value ? 0 : size.Value);
                nativePanes.ColumnDefinitions[1].Width = new(collapsed.Value ? 0 : 6);
                nativeFirst!.Visibility = nativeSeparator!.Visibility = collapsed.Value ? Visibility.Collapsed : Visibility.Visible;
            }
            await Layout(step);
            submissionList?.CheckRetainedSelection();
            if (editor.SelectionStart != 3 || editor.SelectionLength != 7 || nativeCreates != 2 || nativeReleases != 0)
                throw new InvalidOperationException("Editor selection or native ownership changed during layout updates.");
        }
        watch.Stop();
        var updateBytes = GC.GetAllocatedBytesForCurrentThread() - bytes;
        var updateMs = watch.Elapsed.TotalMilliseconds;
        var updateProbes = new { Editor = editorProbe.Snapshot(), Submissions = submissionsProbe.Snapshot() };
        var updateBuilds = builds - mountBuilds;
        var updateRealizedRows = submissionList?.RealizedCount ?? 1000;
        if (submissionList is not null)
            await submissionList.CheckScrollingAndAccessibility(() => Layout(50), listMode == "virtualized");
        var result = new
        {
            Scenario = native ? "native-adapters" : "framework-primitives", Rows = 1000, EditorLines = 1000, Operations = 50,
            Isolation = isolate,
            SubmissionList = listMode, MountRealizedRows = mountRealizedRows, UpdateRealizedRows = updateRealizedRows,
            SubmissionBehaviorChecksPassed = submissionList is not null,
            ProbesEnabled = LayoutProbe.Enabled, MountProbes = mountProbes,
            UpdateProbes = updateProbes,
            MountMilliseconds = mountMs, Milliseconds = updateMs,
            MountAllocatedBytes = mountBytes, AllocatedBytes = updateBytes,
            MountBodyBuilds = mountBuilds, BodyBuilds = updateBuilds,
            NativeCreates = nativeCreates, NativeReleasesDuringUpdates = nativeReleases, Runtime = Environment.Version.ToString()
        };
        foreach (var host in hosts.AsEnumerable().Reverse()) host.Dispose();
        if (nativeReleases != 2) throw new InvalidOperationException("Native controls were not released exactly once.");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(report))!);
        File.WriteAllText(report, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"{result.Scenario}: identical geometry and retained selection/ownership verified for all 50 operations.");
    }
}
