using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Lab_Feedback_WPF;
using Lab_Feedback_WPF.Models;
using Lab_Feedback_WPF.Services;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        var app = new App { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        app.Dispatcher.InvokeAsync(async () =>
        {
            try { await Run(args); app.Shutdown(0); }
            catch (Exception error) { Console.Error.WriteLine(error); app.Shutdown(1); }
        });
        return app.Run();
    }

    private static T Construct<T>(params object[] arguments) => (T)Activator.CreateInstance(typeof(T),
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, arguments, null)!;
    private static T Field<T>(object owner, string name) => (T)owner.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;
    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match) yield return match;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }
    private static LayoutProbe Wrap(FrameworkElement element)
    {
        var parent = VisualTreeHelper.GetParent(element);
        var probe = new LayoutProbe();
        if (parent is Decorator decorator) { decorator.Child = null; probe.Child = element; decorator.Child = probe; }
        else if (parent is Panel panel)
        {
            var index = panel.Children.IndexOf(element);
            panel.Children.RemoveAt(index); probe.Child = element; panel.Children.Insert(index, probe);
        }
        else throw new InvalidOperationException($"Unsupported probe parent: {parent?.GetType().Name}");
        return probe;
    }
    private static async Task Run(string[] args)
    {
        var report = Path.GetFullPath(args[Array.IndexOf(args, "--report") + 1]);
        var folder = Path.GetDirectoryName(report)!;
        Directory.CreateDirectory(folder);
        var database = Path.Combine(folder, Path.GetFileNameWithoutExtension(report) + ".db");
        LayoutProbe.Enabled = args.Contains("--probe-layout");
        var resizeNavigation = args.Contains("--resize-navigation");
        var virtualizeFiles = args.Contains("--virtualize-files");
        var startupWatch = Stopwatch.StartNew();
        var startupBytes = GC.GetAllocatedBytesForCurrentThread();
        // Exercise the actual app-owned opt-in, not a separately linked prototype.
        typeof(App).GetMethod("ConfigureExperimentalCleanup", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(Application.Current, [args]);
        var window = Construct<MainWindow>(Construct<AssignmentPersistenceService>(database), Construct<CommentPersistenceService>(database));
        var root = (FrameworkElement)window.Content;
        window.Content = null;
        var students = Field<ListBox>(window, "listBoxStudents");
        var files = Field<TreeView>(window, "fileTreeView");
        if (args.Contains("--full-files"))
        {
            VirtualizingPanel.SetIsVirtualizing(files, false);
            files.ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(StackPanel)));
            var fullStyle = new Style(typeof(TreeViewItem), files.ItemContainerStyle);
            fullStyle.Setters.Add(new Setter(ItemsControl.ItemsPanelProperty,
                new ItemsPanelTemplate(new FrameworkElementFactory(typeof(StackPanel)))));
            files.ItemContainerStyle = fullStyle;
        }
        if (virtualizeFiles && !VirtualizingPanel.GetIsVirtualizing(files))
        {
            VirtualizingPanel.SetIsVirtualizing(files, true);
            VirtualizingPanel.SetVirtualizationMode(files, VirtualizationMode.Recycling);
            ScrollViewer.SetCanContentScroll(files, true);
            files.ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(VirtualizingStackPanel)));
            var itemStyle = new Style(typeof(TreeViewItem), files.ItemContainerStyle);
            itemStyle.Setters.Add(new Setter(TreeViewItem.IsSelectedProperty,
                new System.Windows.Data.Binding(nameof(FileSystemItem.IsSelected)) { Mode = System.Windows.Data.BindingMode.TwoWay }));
            itemStyle.Setters.Add(new Setter(ItemsControl.ItemsPanelProperty,
                new ItemsPanelTemplate(new FrameworkElementFactory(typeof(VirtualizingStackPanel)))));
            files.ItemContainerStyle = itemStyle;
        }
        var editor = Field<ICSharpCode.AvalonEdit.TextEditor>(window, "codeEditor");
        Field<Border>(window, "emptyStateOverlay").Visibility = Visibility.Collapsed;
        students.ItemsSource = Enumerable.Range(0, 1000).Select(i => new Student("Student", $"Synthetic {i:D4}", i.ToString(), null)).ToArray();
        students.SelectedIndex = 7;
        for (var i = 0; i < 1000; i++) files.Items.Add(new FileSystemItem(Path.Combine(folder, $"Synthetic{i:D4}.cs"), false));
        editor.Text = string.Join('\n', Enumerable.Range(0, 1000).Select(i => $"// Synthetic editor line {i}"));
        editor.Select(3, 7);
        var studentProbe = Wrap(students);
        var fileProbe = Wrap(files);
        var editorProbe = Wrap(editor);
        Grid? paneGrid = null;
        var rootProbe = new LayoutProbe { Child = root };
        using var source = new HwndSource(new HwndSourceParameters("Consumer layout diagnostic")
        { Width = 1440, Height = 960, PositionX = -10000, PositionY = -10000, WindowStyle = unchecked((int)0x80000000) });
        source.RootVisual = rootProbe;
        double dispatcherMs = 0, explicitLayoutMs = 0;
        async Task Layout(int step)
        {
            var phase = Stopwatch.GetTimestamp();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            dispatcherMs += Stopwatch.GetElapsedTime(phase).TotalMilliseconds;
            phase = Stopwatch.GetTimestamp();
            var size = step % 2 == 0 ? new Size(1440, 960) : new Size(1200, 800);
            rootProbe.Measure(size); rootProbe.Arrange(new Rect(size)); rootProbe.UpdateLayout();
            explicitLayoutMs += Stopwatch.GetElapsedTime(phase).TotalMilliseconds;
        }
        int Realized(ItemsControl control) => Enumerable.Range(0, control.Items.Count).Count(i => control.ItemContainerGenerator.ContainerFromIndex(i) is not null);
        var watch = Stopwatch.StartNew(); var bytes = GC.GetAllocatedBytesForCurrentThread();
        await Layout(0);
        watch.Stop();
        var mountMs = watch.Elapsed.TotalMilliseconds;
        var mountBytes = GC.GetAllocatedBytesForCurrentThread() - bytes;
        startupWatch.Stop();
        var totalStartupBytes = GC.GetAllocatedBytesForCurrentThread() - startupBytes;
        for (DependencyObject? ancestor = students; ancestor is not null; ancestor = VisualTreeHelper.GetParent(ancestor))
            if (ancestor is Grid { ColumnDefinitions.Count: 6 } grid) { paneGrid = grid; break; }
        if (paneGrid is null) throw new InvalidOperationException("Consumer pane grid was not found.");
        var realizedStudents = Realized(students); var realizedFiles = Realized(files);
        var studentPanel = Descendants<VirtualizingStackPanel>(students).FirstOrDefault();
        var filePanel = Descendants<VirtualizingStackPanel>(files).FirstOrDefault();
        studentProbe.Reset(); fileProbe.Reset(); editorProbe.Reset(); rootProbe.Reset();
        dispatcherMs = explicitLayoutMs = 0;
        bytes = GC.GetAllocatedBytesForCurrentThread(); watch.Restart();
        for (var step = 1; step <= 50; step++)
        {
            if (resizeNavigation) paneGrid.ColumnDefinitions[0].Width = new GridLength(260 + step % 4 * 10);
            await Layout(step);
            if (students.SelectedIndex != 7 || editor.SelectionStart != 3 || editor.SelectionLength != 7)
                throw new InvalidOperationException("Selection changed while resizing the consumer workspace.");
            if (students.ActualHeight <= 0 || files.ActualHeight <= 0 || editor.ActualWidth <= 0 || editor.ActualHeight <= 0)
                throw new InvalidOperationException("Consumer panes lost their finite viewport.");
        }
        watch.Stop();
        var updateBytes = GC.GetAllocatedBytesForCurrentThread() - bytes;
        var measurements = new { StartupMilliseconds = startupWatch.Elapsed.TotalMilliseconds, StartupAllocatedBytes = totalStartupBytes,
            MountLayoutMilliseconds = mountMs, MountLayoutAllocatedBytes = mountBytes,
            UpdateMilliseconds = watch.Elapsed.TotalMilliseconds, UpdateAllocatedBytes = updateBytes,
            DispatcherMilliseconds = dispatcherMs, ExplicitLayoutMilliseconds = explicitLayoutMs,
            Root = rootProbe.Snapshot(), Students = studentProbe.Snapshot(), Files = fileProbe.Snapshot(), Editor = editorProbe.Snapshot() };
        students.ScrollIntoView(students.Items[999]); await Layout(50);
        if (students.ItemContainerGenerator.ContainerFromIndex(999) is not ListBoxItem || students.SelectedIndex != 7)
            throw new InvalidOperationException("Consumer list scrolling lost selection or failed to realize the last student.");
        await FileTreeChecks.Run(files, () => Layout(50), folder, VirtualizingPanel.GetIsVirtualizing(files), editor);
        var result = new { Kind = "actual-consumer-tree-synthetic-data", ProbesEnabled = LayoutProbe.Enabled,
            ExperimentalComCleanup = (bool)typeof(App).GetProperty("ExperimentalCleanupEnabled", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(Application.Current)!,
            ResizeNavigation = resizeNavigation,
            VirtualizeFiles = virtualizeFiles,
            FileTreeBehaviorChecksPassed = true,
            Students = 1000, Files = 1000, EditorLines = 1000, Operations = 50,
            RealizedStudents = realizedStudents, RealizedFiles = realizedFiles, RealizedStudentsAfterScroll = Realized(students),
            StudentVirtualizationEnabled = VirtualizingPanel.GetIsVirtualizing(students), StudentMode = VirtualizingPanel.GetVirtualizationMode(students).ToString(),
            StudentCanContentScroll = ScrollViewer.GetCanContentScroll(students), StudentPanel = studentPanel?.GetType().Name,
            FileVirtualizationEnabled = VirtualizingPanel.GetIsVirtualizing(files), FilePanel = filePanel?.GetType().Name,
            StudentAccessibleName = AutomationProperties.GetName(students), measurements,
            AppAssembly = typeof(MainWindow).Assembly.GetName().Version?.ToString(),
            FrameworkAssembly = root.GetType().Assembly.GetName().Version?.ToString(), Runtime = Environment.Version.ToString() };
        source.RootVisual = null;
        window.Close();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        File.WriteAllText(report, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Consumer: {realizedStudents}/1000 students and {realizedFiles}/1000 files realized; selection/scroll checks passed.");
    }
}
