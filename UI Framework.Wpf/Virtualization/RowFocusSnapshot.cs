using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace UI_Framework.Wpf;

// Keyed framework frames survive sibling reorder. Unkeyed/native segments retain
// positional identity and require the same structure and control types.
// Native islands own IME and any state beyond TextBox selection themselves.
internal sealed record RowFocusSnapshot((int Index, Type Type, string? Key)[] Path, int Start, int Length)
{
    internal static RowFocusSnapshot? Capture(DependencyObject root)
    {
        if (Keyboard.FocusedElement is not Visual focused) return null;
        var path = new List<(int, Type, string?)>();
        DependencyObject current = focused;
        while (!ReferenceEquals(current, root))
        {
            var parent = VisualTreeHelper.GetParent(current);
            if (parent is null) return null;
            var index = 0;
            while (index < VisualTreeHelper.GetChildrenCount(parent) && !ReferenceEquals(VisualTreeHelper.GetChild(parent, index), current)) index++;
            path.Add((index, current.GetType(), FocusIdentity.GetKey(current)));
            current = parent;
        }
        path.Reverse();
        return new(path.ToArray(), focused is TextBox text ? text.SelectionStart : 0,
            focused is TextBox editor ? editor.SelectionLength : 0);
    }

    internal void Restore(DependencyObject root)
    {
        var current = root;
        foreach (var (index, type, key) in Path)
        {
            var count = VisualTreeHelper.GetChildrenCount(current);
            if (key is not null)
            {
                DependencyObject? matching = null;
                for (var child = 0; child < count; child++)
                    if (VisualTreeHelper.GetChild(current, child) is { } frame && FocusIdentity.GetKey(frame) == key)
                    { matching = frame; break; }
                if (matching is null) return;
                current = matching;
            }
            else
            {
                if (index >= count) return;
                current = VisualTreeHelper.GetChild(current, index);
            }
            if (current.GetType() != type) return;
        }
        if (current is not FrameworkElement { Focusable: true, IsEnabled: true } element) return;
        if (element.Focus() && element is TextBox input)
            input.Select(Math.Min(Start, input.Text.Length), Math.Min(Length, input.Text.Length - Math.Min(Start, input.Text.Length)));
    }
}
