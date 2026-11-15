using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using UI_Framework.Wpf;

namespace PackageWorkspace;

internal static class WorkspaceAcceptance
{
    public static void Run(string output, bool virtualized)
    {
        if (Directory.Exists(output)) throw new InvalidOperationException("Use a fresh report directory.");
        Directory.CreateDirectory(output);
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        var failures = new List<string>();
        var cleanup = new WpfComCleanupPolicy(app.Dispatcher, error => failures.Add(error.ToString()));
        var records = new List<object>();
        var weak = new List<WeakReference>();
        Exception? error = null;
        app.Startup += (_, _) =>
        {
            try
            {
                for (var cycle = 0; cycle < 4; cycle++)
                {
                    records.Add(Exercise(output, virtualized, cycle, weak));
                    Flush();
                    GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
                }
            }
            catch (Exception failure) { error = failure; }
            finally { cleanup.Dispose(); app.Shutdown(); }
        };
        app.Run();
        if (error is not null) throw new InvalidOperationException("Workspace acceptance failed.", error);
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        Require(cleanup.IsClosed && cleanup.FailureCount == 0 && failures.Count == 0, "Cleanup policy failed.");
        Require(app.Dispatcher.HasShutdownFinished, "Dispatcher did not finish shutdown.");
        var survivors = weak.Count(w => w.IsAlive);
        Require(survivors == 0, "Disposed host/native editors remain reachable: " + survivors);
        var process = Process.GetCurrentProcess(); process.Refresh();
        var assemblies = new[] { typeof(UI_Framework.View).Assembly, typeof(ViewHost).Assembly }
            .Select(a => new { name = a.GetName().Name, version = a.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
                sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(a.Location))) }).ToArray();
        File.WriteAllText(Path.Combine(output, "report.json"), JsonSerializer.Serialize(new {
            passed = true, virtualized, rows = 100, updatesPerCycle = 100, cycles = 4, warmupCycles = 1,
            records, survivors, cleanupClosed = cleanup.IsClosed, dispatcherShutdown = app.Dispatcher.HasShutdownFinished,
            collectedManagedBytes = GC.GetTotalMemory(true), process.PrivateMemorySize64, process.HandleCount,
            assemblies, runtime = Environment.Version.ToString(), releaseGate = false,
            scope = "Real offscreen windows and routed interactions; not physical keyboard/IME acceptance"
        }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("Package workspace interaction, retention, cleanup and shutdown passed.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object Exercise(string output, bool virtualized, int cycle, List<WeakReference> weak)
    {
        var model = new WorkspaceModel { Virtualized = virtualized };
        var allocated = GC.GetAllocatedBytesForCurrentThread(); var timer = Stopwatch.StartNew();
        using var host = Program.CreateHost(model);
        var window = new Window { Title = "Package workspace acceptance", Content = host, Width = 1040, Height = 760,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false, WindowStartupLocation = WindowStartupLocation.Manual };
        window.Show(); Flush(); host.UpdateLayout(); timer.Stop();
        var mountMs = timer.Elapsed.TotalMilliseconds; var mountBytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
        var editor = Find<TextBox>(host).Single(t => !t.AcceptsReturn);
        editor.Text = "Edited release"; Flush();
        Require(model.Title.Value == "Edited release", "Form binding failed.");
        var native = Find<TextBox>(host).Single(t => t.AcceptsReturn);
        native.Text = "Edited native notes"; native.Select(2, 4); Flush();
        Require(model.Notes.Value == "Edited native notes", "Native editing failed.");
        Click(host, "Work item 003"); Require(model.Selected.Value == 2, "List selection failed.");
        Click(host, "Next item"); Require(model.Selected.Value == 3, "Selection action failed.");
        window.Width = 800; model.Pane.Value = 240; Flush(); host.UpdateLayout();
        Require(ReferenceEquals(editor, Find<TextBox>(host).Single(t => !t.AcceptsReturn)), "Resize replaced editor.");
        Require(ReferenceEquals(native, Find<TextBox>(host).Single(t => t.AcceptsReturn)) && native.SelectionLength == 4,
            "Resize replaced native island or selection.");
        window.Width = 1040; Flush();
        allocated = GC.GetAllocatedBytesForCurrentThread(); timer.Restart(); var builds = model.Builds;
        for (var i = 0; i < 100; i++) { model.Selected.Value = i; Flush(); }
        timer.Stop(); var updateBytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
        var updateMs = timer.Elapsed.TotalMilliseconds; var updateBuilds = model.Builds - builds;
        Require(model.Creates == 1 && model.Releases == 0, "Ordinary updates remounted native control.");
        if (cycle == 0)
        {
            var bitmap = new RenderTargetBitmap((int)host.ActualWidth, (int)host.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(host); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = File.Create(Path.Combine(output, "workspace.png")); encoder.Save(stream);
        }
        Click(host, "Review"); Require(model.Releases == 1, "Navigation did not release native island.");
        Click(host, "Back"); Require(model.Title.Value == "Edited release" && model.Notes.Value == "Edited native notes", "Navigation lost model edits.");
        Require(Find<TextBox>(host).Single(t => t.AcceptsReturn).Text == model.Notes.Value, "Native remount did not restore model.");
        window.Content = null; host.Dispose(); host.Dispose(); window.Close(); Flush();
        Require(model.Creates == model.Releases && model.Mounts == model.Unmounts, "Unbalanced disposal.");
        weak.Add(new WeakReference(host)); weak.AddRange(model.NativeReferences);
        return new { cycle, warmup = cycle == 0, mountMs, mountBytes, updateMs, updateBytes, updateBuilds,
            model.Mounts, model.Unmounts, model.Creates, model.Updates, model.Releases };
    }

    private static void Click(DependencyObject root, string label)
    { Find<Button>(root).Single(b => Equals(b.Content, label)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Flush(); }
    private static void Flush() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static IEnumerable<T> Find<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T item) yield return item;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            foreach (var child in Find<T>(VisualTreeHelper.GetChild(root, i))) yield return child;
    }
}
