using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        var count = int.Parse(args[0]);
        var scenario = args[1];
        var mode = args.Length > 2 ? args[2] : "refresh";
        if (mode is not ("refresh" or "notified-storage" or "storage" or "layout" or "full-layout")) throw new ArgumentException("Unknown diagnostic mode");
        if (mode is "storage" or "notified-storage" && scenario == "filter") throw new ArgumentException("Storage attribution uses reorder workloads only");
        var layout = mode is "layout" or "full-layout";
        var ids = Enumerable.Range(0, count).ToArray();
        var states = layout ? ids.Select(id => new State<string>($"row {id}")).ToArray() : [];
        var counters = new ShuffleCounters();
        View[] Describe() => ids.Select(i => layout
            ? Component<ShuffleRow>(row => { row.Value = states[i]; row.Counters = counters; }).Id(i.ToString()).Memo(i)
            : Text(i.ToString()).Id(i.ToString())).ToArray();
        using var cleanup = new WpfComCleanupPolicy(Dispatcher.CurrentDispatcher);
        var mountAllocated = GC.GetAllocatedBytesForCurrentThread();
        var mountClock = Stopwatch.StartNew();
        using var host = new ViewHost(() => mode == "full-layout" ? Scroll(VStack(Describe())) : VirtualList(Describe(), 300));
        Window? window = null;
        try
        {
            if (layout)
            {
                window = new Window { Content = host, Width = 640, Height = 380, ShowInTaskbar = false, ShowActivated = false, Left = -20000, Top = -20000 };
                window.Show();
                FlushLayout(host);
            }
            mountClock.Stop();
            mountAllocated = GC.GetAllocatedBytesForCurrentThread() - mountAllocated;
            var frame = (Border)host.Content;
            var source = mode == "full-layout"
                ? (IList)((StackPanel)((Border)((ScrollViewer)frame.Child).Content).Child).Children
                : (IList)((ItemsControl)frame.Child).ItemsSource;
            var original = source.Cast<object>().ToArray();
            // Diagnostic only: bind the actual collection's Move before measurement.
            // This introduces no private-state writes or production instrumentation.
            Action<int, int>? move = mode is "storage" or "notified-storage"
                ? (Action<int, int>)source.GetType().GetMethod("Move", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
                    .CreateDelegate(typeof(Action<int, int>), source) : null;
            if (mode == "storage") ((ItemsControl)frame.Child).ItemsSource = null;
            var moves = 0; var inserts = 0; var removes = 0; var resets = 0;
            if (source is INotifyCollectionChanged notifications)
                notifications.CollectionChanged += (_, e) =>
                {
                    if (e.Action == NotifyCollectionChangedAction.Move) moves++;
                    if (e.Action == NotifyCollectionChangedAction.Add) inserts++;
                    if (e.Action == NotifyCollectionChangedAction.Remove) removes++;
                    if (e.Action == NotifyCollectionChangedAction.Reset) resets++;
                };
            ids = scenario switch
            {
                "rotate" => ids.Skip(1).Append(ids[0]).ToArray(),
                "reverse" => ids.Reverse().ToArray(),
                "shuffle" => ids.ToArray(),
                "interleave" => ids.Where(i => i % 2 == 1).Concat(ids.Where(i => i % 2 == 0)).ToArray(),
                "filter" => ids.Where(i => i % 2 == 0).ToArray(),
                _ => throw new ArgumentException("rotate, reverse, shuffle, interleave or filter")
            };
            if (scenario == "shuffle") { var random = new Random(42); random.Shuffle(ids); }
            var builds = counters.Builds; var mounts = counters.Mounts; var unmounts = counters.Unmounts;
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            var clock = Stopwatch.StartNew();
            if (move is null) host.Refresh();
            else
                for (var i = 0; i < ids.Length; i++)
                {
                    var desired = original[ids[i]];
                    if (!ReferenceEquals(source[i], desired)) move(source.IndexOf(desired), i);
                }
            if (layout) FlushLayout(host);
            clock.Stop();
            allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
            if (source.Count != ids.Length || ids.Where((id, index) => !ReferenceEquals(original[id], source[index])).Any())
                throw new InvalidOperationException("Row identity or order changed incorrectly.");
            var result = new { count, scenario, mode, milliseconds = clock.Elapsed.TotalMilliseconds, allocated, moves, inserts, removes, resets,
                collectionNotificationsAvailable = source is INotifyCollectionChanged,
                mountMilliseconds = mountClock.Elapsed.TotalMilliseconds, mountAllocated, mountBuilds = builds,
                builds = counters.Builds - builds, mounts = counters.Mounts - mounts, unmounts = counters.Unmounts - unmounts,
                nativeEditors = layout ? Descendants<TextBox>(host).Count() : 0,
                scope = "Supplemental diagnostic: layout/full-layout include real-window editing controls; storage excludes WPF listeners; notified-storage adds listeners; refresh adds descriptions. Not a release gate.",
                runtime = Environment.Version.ToString() };
            host.Dispose();
            if (counters.Mounts != counters.Unmounts) throw new InvalidOperationException("Component lifetime imbalance.");
            Console.WriteLine(JsonSerializer.Serialize(result));
        }
        finally { window?.Close(); }
    }

    private static void FlushLayout(ViewHost host)
    {
        host.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        host.UpdateLayout();
        host.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T match) yield return match;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            foreach (var child in Descendants<T>(VisualTreeHelper.GetChild(root, i))) yield return child;
    }
}
