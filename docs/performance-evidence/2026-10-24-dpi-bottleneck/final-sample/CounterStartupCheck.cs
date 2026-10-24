using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using UI_Framework.Wpf;

namespace StressLab;

internal static class CounterStartupCheck
{
    internal static int Run(Window window, ViewHost host, WorkspaceModel model, Stopwatch startup,
        long allocation, bool cold, string[] args)
    {
        window.Show();
        Flush();
        window.UpdateLayout();
        startup.Stop();
        var bytes = GC.GetAllocatedBytesForCurrentThread() - allocation;
        var startupBuilds = model.Counters.Builds;
        if (PresentationSource.FromVisual(host) is null) throw new InvalidOperationException("Startup did not attach the host.");
        var index = Array.IndexOf(args, "--report");
        var path = Path.GetFullPath(index >= 0 ? args[index + 1] : "artifacts/startup-check.json");
        if (File.Exists(path)) throw new InvalidOperationException("Use a new startup report path.");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var geometry = new ComparisonPhaseTrace();
        geometry.CaptureGeometry(host, "startup");
        var item = model.Items[0];
        var input = Find<TextBox>(host).First(value => value.Text == item.Title.Value);
        input.Text = "Startup binding check";
        Flush();
        if (item.Title.Value != input.Text) throw new InvalidOperationException("Native editing failed after early presentation initialization.");
        window.Close();
        host.Dispose();
        if (model.Counters.Active != 0) throw new InvalidOperationException("Startup check leaked components.");
        File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            Kind = "actual-window-startup-check", ColdWindowStartup = cold, Rows = model.Items.Count,
            Milliseconds = startup.Elapsed.TotalMilliseconds, AllocatedBytes = bytes,
            BodyBuilds = startupBuilds, NativeEditingPassed = true, ActiveAfterDisposal = model.Counters.Active,
            Runtime = Environment.Version.ToString()
        }, new JsonSerializerOptions { WriteIndented = true }));
        geometry.Save(path);
        return 0;
    }

    private static void Flush() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);

    private static IEnumerable<T> Find<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T match) yield return match;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
            foreach (var child in Find<T>(VisualTreeHelper.GetChild(root, index))) yield return child;
    }
}
