using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

internal static class TestVisualTree
{
    internal static void Flush() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    internal static void Layout(FrameworkElement element)
    {
        Flush();
        element.Measure(new Size(500, 300));
        element.Arrange(new Rect(0, 0, 500, 300));
        element.UpdateLayout();
        Flush();
        element.UpdateLayout();
    }
    internal static IEnumerable<T> Find<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T match) yield return match;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            foreach (var child in Find<T>(VisualTreeHelper.GetChild(root, i))) yield return child;
    }
}
