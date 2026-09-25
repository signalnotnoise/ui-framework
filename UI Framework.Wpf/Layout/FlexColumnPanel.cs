using System.Windows;
using System.Windows.Controls;
using UI_Framework;

namespace UI_Framework.Wpf;

internal sealed class FlexColumnPanel : Grid
{
    internal void Update(IReadOnlyList<View> children, double gap)
    {
        var count = Math.Max(0, children.Count * 2 - 1);
        while (RowDefinitions.Count > count) RowDefinitions.RemoveAt(RowDefinitions.Count - 1);
        while (RowDefinitions.Count < count) RowDefinitions.Add(new RowDefinition());
        for (var i = 0; i < count; i++)
        {
            var child = children[i / 2];
            var height = i % 2 != 0 ? new GridLength(gap)
                : !double.IsNaN(child.DesiredHeight) || child.FlexWeight == 0 ? GridLength.Auto
                : new GridLength(child.FlexWeight, GridUnitType.Star);
            if (RowDefinitions[i].Height != height) RowDefinitions[i].Height = height;
        }
    }
}
