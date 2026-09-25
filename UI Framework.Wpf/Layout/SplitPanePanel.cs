using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using UI_Framework;

namespace UI_Framework.Wpf;

internal sealed class SplitPanePanel : Grid
{
    private readonly GridSplitter splitter = new()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch,
        ResizeBehavior = GridResizeBehavior.PreviousAndNext, KeyboardIncrement = 10, DragIncrement = 1,
        Background = SystemColors.ControlDarkBrush, Focusable = true
    };
    private SplitPaneLayout? layout;
    private bool detached;
    internal SplitPanePanel()
    {
        ClipToBounds = true;
        AutomationProperties.SetName(splitter, "Resize panes");
        Children.Add(splitter);
        splitter.DragCompleted += (_, args) => { if (!args.Canceled) PublishSize(); };
        splitter.KeyUp += (_, args) =>
        {
            if (args.Key is Key.Left or Key.Right or Key.Up or Key.Down) PublishSize();
        };
    }
    internal bool IsChrome(UIElement element) => ReferenceEquals(element, splitter);
    internal void Deactivate() { detached = true; layout = null; }
    internal void Update(SplitPaneLayout next)
    {
        var previous = layout;
        var rebuild = previous is null || previous.Axis != next.Axis;
        layout = next;
        var horizontal = next.Axis == SplitAxis.Horizontal;
        if (rebuild)
        {
            ColumnDefinitions.Clear(); RowDefinitions.Clear();
            for (var i = 0; i < 3; i++)
                if (horizontal) ColumnDefinitions.Add(new ColumnDefinition());
                else RowDefinitions.Add(new RowDefinition());
        }
        splitter.ResizeDirection = horizontal ? GridResizeDirection.Columns : GridResizeDirection.Rows;
        splitter.Cursor = horizontal ? Cursors.SizeWE : Cursors.SizeNS;
        splitter.Visibility = next.FirstCollapsed ? Visibility.Collapsed : Visibility.Visible;
        SetColumn(splitter, horizontal ? 1 : 0); SetRow(splitter, horizontal ? 0 : 1);
        var extent = new GridLength(next.FirstCollapsed ? 0 : Math.Max(next.MinimumFirst, next.FirstExtent));
        var assignExtent = rebuild || previous!.FirstExtent != next.FirstExtent || previous.FirstCollapsed != next.FirstCollapsed
            || previous.MinimumFirst != next.MinimumFirst;
        if (horizontal)
        {
            ColumnDefinitions[0].MinWidth = next.FirstCollapsed ? 0 : next.MinimumFirst;
            ColumnDefinitions[2].MinWidth = next.MinimumSecond;
            if (assignExtent) ColumnDefinitions[0].Width = extent;
            ColumnDefinitions[1].Width = new GridLength(next.FirstCollapsed ? 0 : 6);
            ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star);
        }
        else
        {
            RowDefinitions[0].MinHeight = next.FirstCollapsed ? 0 : next.MinimumFirst;
            RowDefinitions[2].MinHeight = next.MinimumSecond;
            if (assignExtent) RowDefinitions[0].Height = extent;
            RowDefinitions[1].Height = new GridLength(next.FirstCollapsed ? 0 : 6);
            RowDefinitions[2].Height = new GridLength(1, GridUnitType.Star);
        }
    }
    internal void ArrangeChild(FrameworkElement child, int index)
    {
        var horizontal = layout!.Axis == SplitAxis.Horizontal;
        SetColumn(child, horizontal ? index * 2 : 0); SetRow(child, horizontal ? 0 : index * 2);
        child.Visibility = index == 0 && layout.FirstCollapsed ? Visibility.Collapsed : Visibility.Visible;
    }
    private void PublishSize()
    {
        if (detached || layout is not { FirstCollapsed: false } current) return;
        var extent = current.Axis == SplitAxis.Horizontal ? ColumnDefinitions[0].ActualWidth : RowDefinitions[0].ActualHeight;
        if (!double.IsFinite(extent)) return;
        current.Resize(extent);
        if (detached) return;
        var accepted = current.ReadExtent();
        if (detached) return;
        if (!double.IsFinite(accepted) || accepted < 0) throw new InvalidOperationException("Split pane binding returned an invalid extent.");
        // A binding may normalize or reject a resize without notifying state.
        var size = new GridLength(Math.Max(current.MinimumFirst, accepted));
        if (current.Axis == SplitAxis.Horizontal) ColumnDefinitions[0].Width = size;
        else RowDefinitions[0].Height = size;
    }
}
