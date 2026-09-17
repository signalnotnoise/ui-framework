using System.Windows;
using System.Windows.Controls;

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
        row.Saved = host?.Capture();
        row.Presenter = null;
        row = null;
        var old = host;
        host = null;
        Content = null;
        old?.Dispose();
    }
}
