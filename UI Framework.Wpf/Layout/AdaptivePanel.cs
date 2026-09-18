using System.Windows;
using System.Windows.Controls;

namespace UI_Framework.Wpf;

internal sealed class AdaptivePanel : Panel
{
    internal double MinimumColumnWidth { get; set; }
    internal double Gap { get; set; }
    private int Columns(double width) => double.IsInfinity(width) ? Math.Max(1, InternalChildren.Count)
        : Math.Max(1, Math.Min(InternalChildren.Count, (int)Math.Floor((width + Gap) / (MinimumColumnWidth + Gap))));

    protected override Size MeasureOverride(Size available)
    {
        if (InternalChildren.Count == 0) return new Size();
        var columns = Columns(available.Width);
        var width = double.IsInfinity(available.Width) ? MinimumColumnWidth : Math.Max(0, (available.Width - Gap * (columns - 1)) / columns);
        double height = 0, row = 0;
        for (var i = 0; i < InternalChildren.Count; i++)
        {
            var child = InternalChildren[i];
            child.Measure(new Size(width, double.PositiveInfinity));
            row = Math.Max(row, child.DesiredSize.Height);
            if (i % columns == columns - 1 || i == InternalChildren.Count - 1)
            { height += row + (i >= columns ? Gap : 0); row = 0; }
        }
        return new Size(columns * width + (columns - 1) * Gap, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var columns = Columns(finalSize.Width);
        var width = Math.Max(0, (finalSize.Width - Gap * (columns - 1)) / columns);
        double y = 0;
        for (var first = 0; first < InternalChildren.Count; first += columns)
        {
            var count = Math.Min(columns, InternalChildren.Count - first);
            var height = Enumerable.Range(first, count).Max(i => InternalChildren[i].DesiredSize.Height);
            for (var j = 0; j < count; j++) InternalChildren[first + j].Arrange(new Rect(j * (width + Gap), y, width, height));
            y += height + Gap;
        }
        return finalSize;
    }
}
