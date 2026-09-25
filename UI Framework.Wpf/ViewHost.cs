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
    private bool refreshing, faulted;

    public ViewHost(Func<View> body) : this(body, null) { }

    internal ViewHost(Func<View> body, NodeSnapshot? snapshot)
    {
        initialSnapshot = snapshot;
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        VerticalContentAlignment = VerticalAlignment.Stretch;
        session = new(body);
        session.Invalidated += Schedule;
        try { Refresh(); }
        catch (Exception error)
        {
            try { Dispose(); }
            catch (Exception cleanupError)
            {
                throw new AggregateException("Initial render failed and host cleanup also failed.", error, cleanupError);
            }
            throw;
        }
    }

    private void Schedule()
    {
        Dispatcher.VerifyAccess();
        if (queued || disposed || faulted) return;
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
        if (refreshing) throw new InvalidOperationException("A host cannot refresh during its own render or cleanup callback.");
        queued = false;
        refreshing = true;
        try
        {
            var view = session.BuildCandidate(out var commit);
            Validate(view);
            try
            {
                var nextRoot = Patch(root, view, initialSnapshot);
                initialSnapshot = null;
                root = nextRoot;
                Content = nextRoot.Element;
                commit();
                faulted = false;
            }
            catch (Exception error)
            {
                // Native callbacks cannot be rolled back. Never reuse a partially patched tree.
                faulted = true;
                queued = false;
                initialSnapshot = null;
                var failedRoot = root;
                root = null;
                Content = null;
                try { failedRoot?.Dispose(); }
                catch (Exception cleanupError)
                {
                    throw new AggregateException("Rendering failed and tree cleanup also failed.", error, cleanupError);
                }
                throw;
            }
        }
        finally { refreshing = false; }
    }

    private static void Validate(View view)
    {
        ArgumentNullException.ThrowIfNull(view);
        // Explicit patterns compile to value checks without repeated enum metadata lookups.
        // Regression coverage checks every declared value when these enums grow.
        if (view.Kind is not (ViewKind.Text or ViewKind.Button or ViewKind.TextField or ViewKind.VStack
                or ViewKind.HStack or ViewKind.Component or ViewKind.Toggle or ViewKind.Scroll
                or ViewKind.VirtualList or ViewKind.FlexRow or ViewKind.AdaptiveGrid or ViewKind.Navigation
                or ViewKind.TextEditor or ViewKind.PasswordField or ViewKind.Picker or ViewKind.Platform or ViewKind.FlexColumn or ViewKind.SplitPane)
            || view.Horizontal is not (ViewAlignment.Stretch or ViewAlignment.Start or ViewAlignment.Center or ViewAlignment.End)
            || view.Vertical is not (ViewAlignment.Stretch or ViewAlignment.Start or ViewAlignment.Center or ViewAlignment.End)
            || view.ButtonAppearance is not (ButtonStyleKind.Secondary or ButtonStyleKind.Primary or ButtonStyleKind.Quiet)
            || view.Transition is not (NavigationTransition.None or NavigationTransition.FadeSlide))
            throw new InvalidOperationException("View contains an unknown kind, alignment, appearance or transition.");
        if (view.Children is null || view.Options is null || view.Content is null)
            throw new InvalidOperationException("View content and collections cannot be null.");
        static bool Dimension(double value) => double.IsFinite(value) && value >= 0;
        if (!Dimension(view.Gap) || !Dimension(view.Inset) || !Dimension(view.Radius) || !Dimension(view.FlexWeight)
            || !(double.IsNaN(view.DesiredWidth) || Dimension(view.DesiredWidth))
            || !(double.IsNaN(view.DesiredHeight) || Dimension(view.DesiredHeight))
            || !double.IsFinite(view.TextSize) || view.TextSize <= 0
            || !double.IsFinite(view.MinimumColumnWidth) || view.MinimumColumnWidth <= 0
            || view.MaximumLength < 0 || view.UndoHistoryLimit < 0)
            throw new InvalidOperationException("View dimensions and editor limits are invalid.");
        if (view.Kind != ViewKind.Component && (view.ComponentType is not null || view.CreateComponent is not null
            || view.ConfigureComponent is not null || view.IsMemoized))
            throw new InvalidOperationException("Component metadata requires a Component view.");
        if (view.Kind != ViewKind.Platform && view.PlatformContent is not null)
            throw new InvalidOperationException("Platform content requires a Platform view.");
        if (view.Kind is not (ViewKind.VStack or ViewKind.HStack or ViewKind.FlexRow or ViewKind.FlexColumn or ViewKind.SplitPane or ViewKind.AdaptiveGrid
            or ViewKind.Scroll or ViewKind.Navigation or ViewKind.VirtualList) && view.Children.Count != 0)
            throw new InvalidOperationException("This view kind does not accept declarative children.");
        if (view.Kind == ViewKind.Platform && (view.PlatformContent is not NativeViewDescriptor || view.Children.Count != 0))
            throw new InvalidOperationException("Use WpfUI.Native<T>() without declarative children for a native WPF island.");
        if (view.Kind == ViewKind.Navigation && (view.Children.Count == 0 || view.Children.Any(child => string.IsNullOrEmpty(child.Key))))
            throw new InvalidOperationException("Navigation requires at least one uniquely keyed screen.");
        if (view.Kind == ViewKind.VirtualList && (!double.IsFinite(view.DesiredHeight) || view.DesiredHeight <= 0))
            throw new InvalidOperationException("VirtualList requires a finite, positive viewport height.");
        if (view.Kind == ViewKind.Scroll && view.Children.Count != 1)
            throw new InvalidOperationException("A Scroll view requires exactly one child.");
        if (view.Kind == ViewKind.SplitPane)
        {
            if (view.Children.Count != 2 || view.SplitLayout is not { } split
                || split.Axis is not (SplitAxis.Horizontal or SplitAxis.Vertical)
                || !Dimension(split.FirstExtent) || !Dimension(split.MinimumFirst) || !Dimension(split.MinimumSecond)
                || split.Resize is null || split.ReadExtent is null)
                throw new InvalidOperationException("SplitPane requires two children, valid extents and a size binding.");
        }
        else if (view.SplitLayout is not null) throw new InvalidOperationException("Split layout metadata requires a SplitPane view.");
        if (view.Kind == ViewKind.Component && (view.ComponentType is null || view.CreateComponent is null))
            throw new InvalidOperationException("Use UI.Component<T>() to describe a component.");
        HashSet<string>? keys = null;
        foreach (var child in view.Children)
        {
            if (child is null) throw new InvalidOperationException("View children cannot be null.");
            if (view.Kind == ViewKind.VirtualList && string.IsNullOrEmpty(child.Key))
                throw new InvalidOperationException("Every VirtualList row requires a stable, nonempty key.");
            if (child.Key is { } key)
            {
                keys ??= [];
                if (!keys.Add(key)) throw new InvalidOperationException($"Duplicate sibling key: {key}");
            }
            Validate(child);
        }
    }

    private static Node Patch(Node? node, View view, NodeSnapshot? snapshot = null)
    {
        var previousNode = node;
        var created = node is null || node.View.Kind != view.Kind || node.View.Key != view.Key
            || node.View.ComponentType != view.ComponentType
            || view.Kind == ViewKind.Platform && node.Control is NativeControlHost retainedNative && !retainedNative.Matches(view.PlatformContent);
        if (created)
        {
            node = null;
        }
        try
        {
            if (created) node = new Node(view, snapshot?.Matches(view) == true ? snapshot : null);
            ArgumentNullException.ThrowIfNull(node);
            var previous = node.View;
            node.View = view; // Event handlers always read the latest description.
            if (created || previous.AccessibleName != view.AccessibleName)
            {
                var target = node.Control is NativeControlHost nativeHost ? nativeHost.Element : node.Control;
                if (view.AccessibleName is null)
                {
                    if (!created || node.Control is not NativeControlHost) target.ClearValue(System.Windows.Automation.AutomationProperties.NameProperty);
                }
                else System.Windows.Automation.AutomationProperties.SetName(target, view.AccessibleName);
            }
            // Avoid boxing and dependency-property work when retained layout is unchanged.
            var padding = new Thickness(view.Inset);
            if (node.Frame.Padding != padding) node.Frame.Padding = padding;
            if (!node.Frame.Width.Equals(view.DesiredWidth)) node.Frame.Width = view.DesiredWidth;
            if (!node.Frame.Height.Equals(view.DesiredHeight)) node.Frame.Height = view.DesiredHeight;
            var radius = new CornerRadius(view.Radius);
            if (node.Frame.CornerRadius != radius) node.Frame.CornerRadius = radius;
            if (created || previous.Horizontal != view.Horizontal)
                node.Frame.HorizontalAlignment = view.Horizontal switch { ViewAlignment.Start => HorizontalAlignment.Left, ViewAlignment.Center => HorizontalAlignment.Center, ViewAlignment.End => HorizontalAlignment.Right, _ => HorizontalAlignment.Stretch };
            if (created || previous.Vertical != view.Vertical)
                node.Frame.VerticalAlignment = view.Vertical switch { ViewAlignment.Start => VerticalAlignment.Top, ViewAlignment.Center => VerticalAlignment.Center, ViewAlignment.End => VerticalAlignment.Bottom, _ => VerticalAlignment.Stretch };
            if (created || previous.Enabled != view.Enabled) node.Frame.IsEnabled = view.Enabled;
            if (node.Control is Button styledButton && (created || previous.ButtonAppearance != view.ButtonAppearance))
                ThemeStyles.SetAppearance(styledButton, view.ButtonAppearance);
            if (created || previous.BackgroundColor != view.BackgroundColor)
                node.Frame.Background = Brush(view.BackgroundColor);
            if (created || previous.ForegroundColor != view.ForegroundColor)
            {
                if (view.ForegroundColor is null) node.Frame.ClearValue(TextElement.ForegroundProperty);
                else TextElement.SetForeground(node.Frame, Brush(view.ForegroundColor));
                if (node.Control is Control native)
                {
                    if (view.ForegroundColor is null) native.ClearValue(Control.ForegroundProperty);
                    else native.Foreground = Brush(view.ForegroundColor);
                }
            }
            switch (node.Control)
            {
                case NavigationSurface navigation:
                    navigation.Update(view.Children, view.Transition);
                    break;
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
                case TextBlock text:
                    if (text.Text != view.Content) text.Text = view.Content;
                    if (!text.FontSize.Equals(view.TextSize)) text.FontSize = view.TextSize;
                    break;
                case Button button:
                    // Content is object-valued: equal labels can be distinct string instances.
                    // Avoid invalidating the presenter when the displayed value is unchanged.
                    if (!Equals(button.Content, view.Content)) button.Content = view.Content;
                    if (!button.FontSize.Equals(view.TextSize)) button.FontSize = view.TextSize;
                    break;
                case CheckBox toggle:
                    if (!Equals(toggle.Content, view.Content)) toggle.Content = view.Content;
                    if (!toggle.FontSize.Equals(view.TextSize)) toggle.FontSize = view.TextSize;
                    if (toggle.IsChecked != view.Checked)
                    {
                        node.Updating = true;
                        try { toggle.IsChecked = view.Checked; }
                        finally { node.Updating = false; }
                    }
                    break;
                case TextBox input:
                    if (!input.FontSize.Equals(view.TextSize)) input.FontSize = view.TextSize;
                    if (created || input.IsReadOnly != view.ReadOnly) input.IsReadOnly = view.ReadOnly;
                    if (created || input.MaxLength != view.MaximumLength) input.MaxLength = view.MaximumLength;
                    var undoLimit = view.ReadOnly ? 0 : view.UndoHistoryLimit;
                    // Reassigning UndoLimit clears history, so only change it when necessary.
                    if (input.UndoLimit != undoLimit) input.UndoLimit = undoLimit;
                    if (created || input.IsUndoEnabled != (undoLimit > 0)) input.IsUndoEnabled = undoLimit > 0;
                    if (input.Text != view.Content)
                    {
                        var caret = input.SelectionStart;
                        node.Updating = true;
                        try
                        {
                            input.Text = view.Content;
                            var desiredCaret = Math.Min(caret, input.Text.Length);
                            if (input.SelectionStart != desiredCaret) input.SelectionStart = desiredCaret;
                        }
                        finally { node.Updating = false; }
                    }
                    break;
                case PasswordBox password:
                    if (!password.FontSize.Equals(view.TextSize)) password.FontSize = view.TextSize;
                    if (created || password.MaxLength != view.MaximumLength) password.MaxLength = view.MaximumLength;
                    node.Updating = true;
                    try { if (password.Password != view.Content) password.Password = view.Content; }
                    finally { node.Updating = false; }
                    break;
                case ComboBox picker:
                    if (!picker.FontSize.Equals(view.TextSize)) picker.FontSize = view.TextSize;
                    node.Updating = true;
                    try
                    {
                        if (created || !previous.Options.SequenceEqual(view.Options)) picker.ItemsSource = view.Options;
                        var selectedIndex = view.SelectedIndex >= 0 && view.SelectedIndex < view.Options.Count ? view.SelectedIndex : -1;
                        if (created || picker.SelectedIndex != selectedIndex) picker.SelectedIndex = selectedIndex;
                    }
                    finally { node.Updating = false; }
                    break;
                case ScrollViewer scroll:
                    var scrollChild = Patch(node.Children.FirstOrDefault(), view.Children[0], node.RestoredChild(view.Children[0], 0));
                    node.Children = [scrollChild];
                    scroll.Content = scrollChild.Element;
                    break;
                case Panel panel:
                    if (panel is SplitPanePanel splitPanel) splitPanel.Update(view.SplitLayout!);
                    else if (panel is FlexColumnPanel column) column.Update(view.Children, view.Gap);
                    else if (panel is Grid grid)
                    {
                        grid.ColumnDefinitions.Clear();
                        foreach (var child in view.Children)
                        {
                            if (grid.ColumnDefinitions.Count > 0) grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(view.Gap) });
                            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = !double.IsNaN(child.DesiredWidth) || child.FlexWeight == 0
                                ? GridLength.Auto : new GridLength(child.FlexWeight, GridUnitType.Star) });
                        }
                    }
                    if (panel is AdaptivePanel adaptive)
                    {
                        adaptive.MinimumColumnWidth = view.MinimumColumnWidth;
                        adaptive.Gap = view.Gap;
                        adaptive.InvalidateMeasure();
                    }
                    var old = node.Children;
                    // Most state updates keep the same siblings in the same order.
                    // Patch those directly instead of allocating reconciliation collections.
                    var sameOrder = old.Count == view.Children.Count && panel.Children.Count == old.Count + (panel is SplitPanePanel ? 1 : 0);
                    for (var i = 0; sameOrder && i < old.Count; i++)
                    {
                        var child = view.Children[i];
                        sameOrder = old[i].View.Kind == child.Kind && old[i].View.Key == child.Key
                            && old[i].View.ComponentType == child.ComponentType
                            && (child.Kind != ViewKind.Platform || old[i].Control is NativeControlHost nativeChild && nativeChild.Matches(child.PlatformContent))
                            && ReferenceEquals(panel.Children[i], old[i].Element);
                    }
                    if (sameOrder)
                    {
                        for (var i = 0; i < old.Count; i++)
                        {
                            Patch(old[i], view.Children[i], node.RestoredChild(view.Children[i], i));
                            SetChildLayout(panel, old[i].Element, i, old.Count, view.Gap);
                        }
                        break;
                    }
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
                        var removedNodes = old.Except(next).ToArray();
                        foreach (var removed in removedNodes) removed.Detach();
                        foreach (var removed in removedNodes) removed.Dispose();
                        var retained = next.Select(n => n.Element).ToHashSet();
                        for (var i = panel.Children.Count - 1; i >= 0; i--)
                            if (!retained.Contains(panel.Children[i]) && !(panel is SplitPanePanel chrome && chrome.IsChrome(panel.Children[i]))) panel.Children.RemoveAt(i);
                        for (var i = 0; i < next.Count; i++)
                        {
                            var element = next[i].Element;
                            if (i >= panel.Children.Count || !ReferenceEquals(panel.Children[i], element))
                            {
                                panel.Children.Remove(element);
                                panel.Children.Insert(i, element);
                            }
                            SetChildLayout(panel, element, i, next.Count, view.Gap);
                        }
                        node.Children = next;
                    }
                    catch (Exception error)
                    {
                        List<Exception>? errors = null;
                        foreach (var added in next.Except(old)) added.Detach();
                        foreach (var added in next.Except(old))
                        {
                            try { added.Dispose(); }
                            catch (Exception cleanupError) { (errors ??= [error]).Add(cleanupError); }
                        }
                        if (errors is not null) throw new AggregateException("Child patch and cleanup failed.", errors);
                        throw;
                    }
                    break;
                case NativeControlHost nativeHost:
                    nativeHost.Update((NativeViewDescriptor)view.PlatformContent!);
                    break;
            }
            node.Restored = null;
            if (created) previousNode?.Dispose();
            return node;
        }
        catch (Exception error)
        {
            if (created && node is not null)
            {
                try { node.Dispose(); }
                catch (Exception cleanupError)
                {
                    throw new AggregateException("Rendering failed and replacement-node cleanup also failed.", error, cleanupError);
                }
            }
            throw;
        }
    }

    private static void SetChildLayout(Panel panel, FrameworkElement element, int index, int count, double gap)
    {
        if (panel is SplitPanePanel split) split.ArrangeChild(element, index);
        else if (panel is FlexColumnPanel) { if (Grid.GetRow(element) != index * 2) Grid.SetRow(element, index * 2); }
        else if (panel is Grid && Grid.GetColumn(element) != index * 2) Grid.SetColumn(element, index * 2);
        var margin = panel is AdaptivePanel or Grid ? new Thickness(0) : panel is StackPanel { Orientation: Orientation.Vertical }
            ? new Thickness(0, 0, 0, index < count - 1 ? gap : 0)
            : new Thickness(0, 0, index < count - 1 ? gap : 0, 0);
        if (element.Margin != margin) element.Margin = margin;
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
        if (refreshing) throw new InvalidOperationException("A host cannot be disposed during its own render callback.");
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

