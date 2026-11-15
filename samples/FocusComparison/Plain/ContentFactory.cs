using System.Windows.Controls;

namespace FocusComparison;

internal static class ContentFactory
{
    internal const string Kind = "plain-wpf";
    internal static ContentLease Create()
    {
        var editors = Enumerable.Range(0, 10).Select(i => new TextBox { Text = $"Editor {i}", Height = 26 }).ToArray();
        var panel = new StackPanel();
        foreach (var editor in editors) panel.Children.Add(editor);
        return new(panel, editors, () => panel.Children.Clear());
    }
}
