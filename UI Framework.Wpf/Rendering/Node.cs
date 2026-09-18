using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Documents;
using System.Windows.Media;
using UI_Framework;
namespace UI_Framework.Wpf;

internal sealed class Node : IDisposable
{
    public View View;
    public readonly FrameworkElement Control;
    public readonly Border Frame;
    public FrameworkElement Element => Frame;
    public List<Node> Children = [];
    public Component? ComponentInstance;
    public bool Updating;
    private bool disposed;
    private bool detached;
    internal NodeSnapshot? Restored;
    internal NodeSnapshot? RestoredChild(View child, int index) => child.Key is { } key
        ? Restored?.Children.FirstOrDefault(saved => saved.Key == key)
        : Restored is { } saved && index < saved.Children.Count && saved.Children[index].Key is null ? saved.Children[index] : null;

    internal NodeSnapshot Capture() => new(View.Kind, View.Key, View.ComponentType, ComponentInstance,
        Control is ViewHost host ? host.Capture() : null, Children.Select(child => child.Capture()).ToArray());

    public Node(View view, NodeSnapshot? snapshot = null)
    {
        Restored = snapshot;
        View = view;
        if (view.Kind == ViewKind.Component)
        {
            ComponentInstance = snapshot?.Instance ?? view.CreateComponent!();
            var host = new ViewHost(() =>
            {
                View.ConfigureComponent?.Invoke(ComponentInstance);
                return ComponentInstance.Body();
            }, snapshot?.Body);
            Control = host;
            Frame = new Border { Child = Control };
            try { ComponentInstance.OnMounted(); }
            catch
            {
                try { host.Dispose(); }
                finally { ComponentInstance.OnUnmounted(); }
                throw;
            }
            return;
        }
        Control = view.Kind switch
        {
            ViewKind.Text => new TextBlock { TextWrapping = TextWrapping.Wrap },
            ViewKind.Button => new Button { Padding = new Thickness(12, 6, 12, 6), HorizontalAlignment = HorizontalAlignment.Left },
            ViewKind.TextField => new TextBox { MinWidth = 40, Padding = new Thickness(6) },
            ViewKind.Toggle => new CheckBox { VerticalAlignment = VerticalAlignment.Center },
            ViewKind.Scroll => new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled },
            ViewKind.VirtualList => new VirtualListControl(),
            ViewKind.VStack => new StackPanel { Orientation = Orientation.Vertical },
            ViewKind.HStack => new StackPanel { Orientation = Orientation.Horizontal },
            ViewKind.FlexRow => new Grid(),
            ViewKind.AdaptiveGrid => new AdaptivePanel(),
            _ => throw new ArgumentOutOfRangeException(nameof(view))
        };
        Frame = new Border { Child = Control };
        if (Control is Button button) button.Click += (_, _) => View.Click?.Invoke();
        if (Control is TextBox input) input.TextChanged += (_, _) =>
        {
            if (Updating) return;
            View.Edit?.Invoke(input.Text);
            // A custom binding may normalize or reject an edit without notifying state.
            var accepted = View.ReadText?.Invoke();
            if (accepted is not null && input.Text != accepted)
            {
                var caret = input.SelectionStart;
                Updating = true;
                try { input.Text = accepted; input.SelectionStart = Math.Min(caret, accepted.Length); }
                finally { Updating = false; }
            }
        };
        if (Control is CheckBox toggle)
        {
            void Changed(object sender, RoutedEventArgs args)
            {
                if (Updating) return;
                View.ToggleChanged?.Invoke(toggle.IsChecked == true);
                if (View.ReadChecked is not { } read) return;
                Updating = true;
                try { toggle.IsChecked = read(); }
                finally { Updating = false; }
            }
            toggle.Checked += Changed;
            toggle.Unchecked += Changed;
        }
    }

    public void Detach()
    {
        if (detached) return;
        detached = true;
        foreach (var child in Children) child.Detach();
        if (Control is ViewHost host) host.Deactivate();
        if (Control is VirtualListControl list) list.Deactivate();
    }

    public void Dispose()
    {
        if (disposed) return;
        Detach();
        disposed = true;
        List<Exception> errors = [];
        void Cleanup(Action action)
        {
            try { action(); }
            catch (Exception error) { errors.Add(error); }
        }
        foreach (var child in Children) Cleanup(child.Dispose);
        Children.Clear();
        if (Control is ViewHost host) Cleanup(host.Dispose);
        if (Control is VirtualListControl list) Cleanup(list.Dispose);
        if (ComponentInstance is { } component) Cleanup(component.OnUnmounted);
        if (errors.Count > 0) throw new AggregateException("Component cleanup failed.", errors);
    }
}
