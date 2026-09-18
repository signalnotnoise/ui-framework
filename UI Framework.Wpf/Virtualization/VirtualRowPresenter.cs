using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace UI_Framework.Wpf;

internal sealed class VirtualRowPresenter : ContentControl
{
    private VirtualRow? row;
    private ViewHost? host;

    internal void Attach(VirtualRow value, double gap)
    {
        Release();
        row = value;
        row.Presenter = this;
        Margin = new Thickness(0, 0, 0, gap);
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        host = new ViewHost(() => value.View, value.Saved);
        value.Saved = null;
        Content = host;
        if (value.RestoreFocus)
        {
            value.RestoreFocus = false;
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(RestoreFocus));
        }
    }

    internal void Refresh(double gap)
    {
        Margin = new Thickness(0, 0, 0, gap);
        host?.Refresh();
    }

    internal void Deactivate() => host?.Deactivate();

    internal void Release()
    {
        if (row is null) return;
        row.RestoreFocus = IsKeyboardFocusWithin;
        row.Saved = host?.Capture();
        row.Presenter = null;
        row = null;
        var old = host;
        host = null;
        Content = null;
        old?.Dispose();
    }

    private void RestoreFocus()
    {
        if (!IsKeyboardFocusWithin && FindFocusable(Content as DependencyObject) is { } focusable)
            focusable.Focus();
    }

    private static FrameworkElement? FindFocusable(DependencyObject? parent)
    {
        if (parent is FrameworkElement element && element.Focusable && element.IsEnabled)
            return element;
        for (var i = 0; parent is not null && i < VisualTreeHelper.GetChildrenCount(parent); i++)
            if (FindFocusable(VisualTreeHelper.GetChild(parent, i)) is { } result)
                return result;
        return null;
    }
}
