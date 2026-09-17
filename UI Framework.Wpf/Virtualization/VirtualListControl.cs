using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;

namespace UI_Framework.Wpf;

// WPF supplies variable-height measurement, pixel scrolling, viewport caching and recycling.
internal sealed class VirtualListControl : ItemsControl, IDisposable
{
    private Dictionary<string, VirtualRow> rows = [];
    private readonly ObservableCollection<VirtualRow> ordered = [];
    private double gap;
    private bool disposed;

    internal VirtualListControl()
    {
        VirtualizingPanel.SetIsVirtualizing(this, true);
        VirtualizingPanel.SetVirtualizationMode(this, VirtualizationMode.Recycling);
        VirtualizingPanel.SetScrollUnit(this, ScrollUnit.Pixel);
        VirtualizingPanel.SetCacheLength(this, new VirtualizationCacheLength(0.5));
        VirtualizingPanel.SetCacheLengthUnit(this, VirtualizationCacheLengthUnit.Page);
        ScrollViewer.SetCanContentScroll(this, true);
        var panel = new FrameworkElementFactory(typeof(VirtualizingStackPanel));
        ItemsPanel = new ItemsPanelTemplate(panel);
        var scroll = new FrameworkElementFactory(typeof(ScrollViewer));
        scroll.SetValue(ScrollViewer.CanContentScrollProperty, true);
        scroll.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
        scroll.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
        scroll.AppendChild(new FrameworkElementFactory(typeof(ItemsPresenter)));
        Template = new ControlTemplate(typeof(ItemsControl)) { VisualTree = scroll };
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        ItemsSource = ordered;
    }

    protected override bool IsItemItsOwnContainerOverride(object item) => false;
    protected override DependencyObject GetContainerForItemOverride() => new VirtualRowPresenter();
    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
        // The presenter owns Content; do not let ItemsControl replace it with the data item.
        ((VirtualRowPresenter)element).Attach((VirtualRow)item, gap);
    }
    protected override void ClearContainerForItemOverride(DependencyObject element, object item) =>
        ((VirtualRowPresenter)element).Release();

    internal void Update(IReadOnlyList<View> views, double spacing)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        gap = spacing;
        var next = new VirtualRow[views.Count];
        var nextByKey = new Dictionary<string, VirtualRow>(views.Count);
        for (var i = 0; i < views.Count; i++)
        {
            var view = views[i];
            var row = rows.GetValueOrDefault(view.Key!) ?? new VirtualRow(view);
            row.View = view;
            next[i] = row;
            nextByKey.Add(view.Key!, row);
        }

        // Detach all removed readers before invoking any user unmount hook.
        var removed = rows.Where(pair => !nextByKey.ContainsKey(pair.Key)).Select(pair => pair.Value).ToArray();
        foreach (var row in removed) row.Presenter?.Deactivate();
        for (var i = ordered.Count - 1; i >= 0; i--)
        {
            var row = ordered[i];
            if (nextByKey.ContainsKey(row.View.Key!)) continue;
            row.Presenter?.Release();
            ordered.RemoveAt(i);
            row.Saved = null;
        }

        // Preserve the ItemsSource and issue only the required insert/move notifications.
        for (var i = 0; i < next.Length; i++)
        {
            if (i < ordered.Count && ReferenceEquals(ordered[i], next[i])) continue;
            var current = ordered.IndexOf(next[i]);
            if (current < 0) ordered.Insert(i, next[i]);
            else ordered.Move(current, i);
        }
        rows = nextByKey;
        foreach (var row in next) row.Presenter?.Refresh(gap);
    }

    internal void Deactivate()
    {
        foreach (var row in rows.Values) row.Presenter?.Deactivate();
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Deactivate();
        foreach (var row in rows.Values) row.Presenter?.Release();
        ItemsSource = null;
        rows.Clear();
        ordered.Clear();
    }
}
