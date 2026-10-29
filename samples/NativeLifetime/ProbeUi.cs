using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace NativeLifetime;

internal static class ProbeUi
{
    internal static async Task Flush(Window window)
    {
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        window.UpdateLayout();
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
    }
    internal static IEnumerable<T> Find<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T item) yield return item;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            foreach (var child in Find<T>(VisualTreeHelper.GetChild(root, i))) yield return child;
    }
    internal static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
    internal static Window Window(string title) => new() { Title = title, Width = 640, Height = 420,
        Left = 100, Top = 100, ShowActivated = false };
    internal static async Task Collect(Window window)
    {
        await Flush(window);
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        await Flush(window);
    }
}
