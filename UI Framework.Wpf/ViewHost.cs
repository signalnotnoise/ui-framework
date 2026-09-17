using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Documents;
using System.Windows.Media;
using UI_Framework;
namespace UI_Framework.Wpf;

public sealed class ViewHost : ContentControl, IDisposable
{
    private static readonly bool ignoreMemo = AppContext.TryGetSwitch("UI_Framework.IgnoreMemo", out var enabled) && enabled;
    private readonly ViewSession session;
    private Node? root;
    private NodeSnapshot? initialSnapshot;
    private bool queued, disposed;

    public ViewHost(Func<View> body) : this(body, null) { }

    internal ViewHost(Func<View> body, NodeSnapshot? snapshot)
    {
        initialSnapshot = snapshot;
        session = new(body);
        session.Invalidated += Schedule;
        try { Refresh(); }
        catch { Dispose(); throw; }
    }

    private void Schedule()
    {
        Dispatcher.VerifyAccess();
        if (queued || disposed) return;
        queued = true;
        Dispatcher.BeginInvoke(DispatcherPriority.DataBind, new Action(() =>
        {
            if (queued && !disposed) Refresh();
        }));
    }

    public void Refresh()
    {
        Dispatcher.VerifyAccess();
        ObjectDisposedException.ThrowIf(disposed, this);
        queued = false;
        var view = session.Build();
        Validate(view);
        root = Patch(root, view, initialSnapshot);
        initialSnapshot = null;
        Content = root.Element;
    }

    private static void Validate(View view)
    {
        if (view.Kind == ViewKind.VirtualList && (!double.IsFinite(view.DesiredHeight) || view.DesiredHeight <= 0))
            throw new InvalidOperationException("VirtualList requires a finite, positive viewport height.");
        if (view.Kind == ViewKind.Scroll && view.Children.Count != 1)
            throw new InvalidOperationException("A Scroll view requires exactly one child.");
        if (view.Kind == ViewKind.Component && (view.ComponentType is null || view.CreateComponent is null))
            throw new InvalidOperationException("Use UI.Component<T>() to describe a component.");
        var keys = new HashSet<string>();
        foreach (var child in view.Children)
        {
            if (view.Kind == ViewKind.VirtualList && string.IsNullOrEmpty(child.Key))
                throw new InvalidOperationException("Every VirtualList row requires a stable, nonempty key.");
            if (child.Key is { } key && !keys.Add(key))
                throw new InvalidOperationException($"Duplicate sibling key: {key}");
            Validate(child);
        }
    }

    private static Node Patch(Node? node, View view, NodeSnapshot? snapshot = null)
    {
        var created = node is null || node.View.Kind != view.Kind || node.View.Key != view.Key
            || node.View.ComponentType != view.ComponentType;
        if (created)
        {
            node?.Dispose();
            node = new Node(view, snapshot?.Matches(view) == true ? snapshot : null);
        }
        ArgumentNullException.ThrowIfNull(node);
        var previous = node.View;
        node.View = view; // Event handlers always read the latest description.
        node.Frame.Padding = new Thickness(view.Inset);
        node.Frame.Width = view.DesiredWidth;
        node.Frame.Height = view.DesiredHeight;
        node.Frame.CornerRadius = new CornerRadius(view.Radius);
        if (created || previous.BackgroundColor != view.BackgroundColor)
            node.Frame.Background = Brush(view.BackgroundColor);
        if (created || previous.ForegroundColor != view.ForegroundColor)
        {
            if (view.ForegroundColor is null) node.Frame.ClearValue(TextElement.ForegroundProperty);
            else TextElement.SetForeground(node.Frame, Brush(view.ForegroundColor));
        }
        switch (node.Control)
        {
            case VirtualListControl list:
                list.Update(view.Children, view.Gap);
                break;
            case ViewHost componentHost:
                if (!created && (ignoreMemo || componentHost.queued || !view.IsMemoized || !previous.IsMemoized
                    || !Equals(view.MemoInputs, previous.MemoInputs)))
                {
                    componentHost.Refresh();
                }
                break;
            case TextBlock text: text.Text = view.Content; text.FontSize = view.TextSize; break;
            case Button button: button.Content = view.Content; button.FontSize = view.TextSize; break;
            case CheckBox toggle:
                toggle.Content = view.Content;
                toggle.FontSize = view.TextSize;
                node.Updating = true;
                try { toggle.IsChecked = view.Checked; }
                finally { node.Updating = false; }
                break;
            case TextBox input:
                input.FontSize = view.TextSize;
                if (input.Text != view.Content)
                {
                    var caret = input.SelectionStart;
                    node.Updating = true;
                    try { input.Text = view.Content; input.SelectionStart = Math.Min(caret, input.Text.Length); }
                    finally { node.Updating = false; }
                }
                break;
            case ScrollViewer scroll:
                var scrollChild = Patch(node.Children.FirstOrDefault(), view.Children[0], node.RestoredChild(view.Children[0], 0));
                node.Children = [scrollChild];
                scroll.Content = scrollChild.Element;
                break;
            case StackPanel panel:
                var old = node.Children;
                var keyed = old.Where(n => n.View.Key != null).ToDictionary(n => n.View.Key!);
                var next = new List<Node>();
                try
                {
                    for (var i = 0; i < view.Children.Count; i++)
                    {
                        var child = view.Children[i];
                        Node? match = child.Key is { } key ? keyed.GetValueOrDefault(key)
                            : i < old.Count && old[i].View.Key is null ? old[i] : null;
                        next.Add(Patch(match, child, node.RestoredChild(child, i)));
                    }
                }
                catch
                {
                    foreach (var added in next.Except(old)) added.Dispose();
                    throw;
                }
                var removedNodes = old.Except(next).ToArray();
                foreach (var removed in removedNodes) removed.Detach();
                foreach (var removed in removedNodes) removed.Dispose();
                var retained = next.Select(n => n.Element).ToHashSet();
                for (var i = panel.Children.Count - 1; i >= 0; i--)
                    if (!retained.Contains(panel.Children[i])) panel.Children.RemoveAt(i);
                for (var i = 0; i < next.Count; i++)
                {
                    var element = next[i].Element;
                    if (i >= panel.Children.Count || !ReferenceEquals(panel.Children[i], element))
                    {
                        panel.Children.Remove(element);
                        panel.Children.Insert(i, element);
                    }
                    element.Margin = panel.Orientation == Orientation.Vertical
                        ? new Thickness(0, 0, 0, i < next.Count - 1 ? view.Gap : 0)
                        : new Thickness(0, 0, i < next.Count - 1 ? view.Gap : 0, 0);
                }
                node.Children = next;
                break;
        }
        node.Restored = null;
        return node;
    }

    private static Brush? Brush(string? color)
    {
        if (color is null) return null;
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        brush.Freeze();
        return brush;
    }

    public void Dispose()
    {
        Dispatcher.VerifyAccess();
        Deactivate();
        var previousRoot = root;
        root = null;
        Content = null;
        previousRoot?.Dispose();
    }

    // Detach the entire subtree before running any user cleanup hook. A hook may pump
    // dispatcher messages; no sibling scheduled for removal should render during it.
    internal void Deactivate()
    {
        if (disposed) return;
        disposed = true;
        queued = false;
        session.Dispose();
        root?.Detach();
    }

    internal NodeSnapshot? Capture() => root?.Capture();


}

