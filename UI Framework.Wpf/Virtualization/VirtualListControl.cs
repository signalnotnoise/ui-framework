using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation.Peers;

namespace UI_Framework.Wpf;

// WPF supplies variable-height measurement, pixel scrolling, viewport caching and recycling.
internal sealed class VirtualListControl : ItemsControl, IDisposable
{
    private Dictionary<string, VirtualRow> rows = [];
    private readonly VirtualRowCollection ordered = new();
    private double gap;
    private bool disposed;
    private IReadOnlyDictionary<string, NodeSnapshot>? initialRows;

    internal VirtualListControl(VirtualListSnapshot? snapshot = null)
    {
        initialRows = snapshot?.Rows;
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

    protected override AutomationPeer OnCreateAutomationPeer() => new VirtualListAutomationPeer(this);
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
        var containsNewRows = false;
        // An empty collection needs no permutation buffer. Validate all keys before
        // publishing rows, then insert in description order using the keyed map.
        var next = ordered.Count == 0 ? null : new VirtualRow[views.Count];
        var nextByKey = new Dictionary<string, VirtualRow>(views.Count);
        for (var i = 0; i < views.Count; i++)
        {
            var view = views[i];
            var row = rows.GetValueOrDefault(view.Key!);
            if (row is null)
            {
                containsNewRows = true;
                row = new VirtualRow(view) { Saved = initialRows?.GetValueOrDefault(view.Key!) };
            }
            row.View = view;
            row.TargetIndex = i;
            if (next is not null) next[i] = row;
            nextByKey.Add(view.Key!, row);
        }

        if (next is null)
        {
            for (var i = 0; i < views.Count; i++) ordered.Insert(i, nextByKey[views[i].Key!]);
            rows = nextByKey;
            initialRows = null;
            return;
        }

        // Detach all removed readers before invoking any user unmount hook.
        List<VirtualRow>? removed = null;
        var previousIndex = 0;
        foreach (var row in ordered)
        {
            if (!nextByKey.ContainsKey(row.View.Key!))
            {
                row.TargetIndex = previousIndex;
                (removed ??= []).Add(row);
            }
            previousIndex++;
        }
        if (removed is not null)
        {
            foreach (var row in removed) row.Presenter?.Deactivate();
            // Descending deletion keeps the recorded indices valid and avoids
            // another rank lookup for every removed row.
            for (var i = removed.Count - 1; i >= 0; i--)
            {
                var row = removed[i];
                row.Presenter?.Release();
                ordered.RemoveAt(row.TargetIndex);
                row.Saved = null;
            }
        }

        // Preserve the ItemsSource and retained row objects; never reset for a reorder.
        for (var i = 0; i < next.Length; i++)
        {
            i = ordered.SkipMatching(next, i);
            if (i == next.Length) break;
            var desired = next[i];
            // A newly described key cannot be in the retained collection. In particular,
            // initial mount and append must not scan every previously inserted row.
            if (containsNewRows && !rows.ContainsKey(desired.View.Key!))
            {
                ordered.Insert(i, desired);
                continue;
            }
            // A single forward move leaves its successor at the destination we need
            // next. Inactive presenters may also move out of the realized viewport.
            // Active/nested editors keep the conservative path because a move can
            // detach their presenter before composition completion.
            if (i + 1 < ordered.Count && ReferenceEquals(ordered[i + 1], desired))
            {
                var displaced = ordered[i];
                var destination = Math.Min(displaced.TargetIndex, ordered.Count - 1);
                if (destination > i && (displaced.Presenter is null
                    || (ItemContainerGenerator.ContainerFromIndex(destination) is null
                        && displaced.Presenter.CanMoveOutsideViewport)))
                    ordered.Move(i, destination);
            }
            if (ReferenceEquals(ordered[i], desired)) continue;
            ordered.Move(ordered.IndexOf(desired), i);
        }
        rows = nextByKey;
        initialRows = null;
        foreach (var row in next) row.Presenter?.Refresh(gap);
    }

    internal void Deactivate()
    {
        foreach (var row in ordered) row.Presenter?.Deactivate();
    }

    internal VirtualListSnapshot Capture()
    {
        var snapshots = new Dictionary<string, NodeSnapshot>();
        foreach (var (key, row) in rows)
            if ((row.Presenter?.Capture() ?? row.Saved) is { } snapshot) snapshots.Add(key, snapshot);
        return new(snapshots);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Deactivate();
        List<Exception>? errors = null;
        foreach (var row in ordered)
        {
            try { row.Presenter?.Release(); }
            catch (Exception error) { (errors ??= []).Add(error); }
        }
        ItemsSource = null;
        rows.Clear();
        ordered.Clear();
        initialRows = null;
        if (errors is not null) throw new AggregateException("Virtual row cleanup failed.", errors);
    }
}
