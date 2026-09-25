namespace UI_Framework;

/// <summary>First-pane extent in pixels; horizontal means side by side, vertical means stacked.</summary>
public sealed record SplitPaneLayout(double FirstExtent, Action<double> Resize, Func<double> ReadExtent,
    SplitAxis Axis = SplitAxis.Horizontal, double MinimumFirst = 100, double MinimumSecond = 100,
    bool FirstCollapsed = false);
