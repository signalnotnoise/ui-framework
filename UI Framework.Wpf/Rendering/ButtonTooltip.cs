using System.Windows;
using System.Windows.Controls;
using UI_Framework;

namespace UI_Framework.Wpf;

internal static class ButtonTooltip
{
    internal static void Patch(Button button, View view)
    {
        if (view.TooltipText is { } text)
        {
            // A local null suppresses style-provided hints too. Compare local
            // values so an equal style value cannot accidentally own an override.
            var content = text.Length == 0 ? null : text;
            if (!Equals(button.ReadLocalValue(FrameworkElement.ToolTipProperty), content))
                button.ToolTip = content;
        }
        else if (view.Icon is null)
        {
            button.ClearValue(FrameworkElement.ToolTipProperty);
        }
        else
        {
            // Preserve the existing icon-only accessible-name fallback and
            // labelled-icon behavior. Presentation/timing remain native WPF.
            var fallback = string.IsNullOrEmpty(view.Content) ? view.AccessibleName : null;
            if (!Equals(button.ToolTip, fallback)) button.ToolTip = fallback;
        }
    }
}
