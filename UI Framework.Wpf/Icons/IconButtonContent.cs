using System.Windows;
using System.Windows.Controls;
using UI_Framework;

namespace UI_Framework.Wpf;

internal sealed class IconButtonContent : DockPanel
{
    private static readonly DependencyProperty labelGapProperty = DependencyProperty.Register(
        "LabelGap", typeof(double), typeof(IconButtonContent),
        new FrameworkPropertyMetadata(IconTokens.Default.LabelGap, static (owner, _) => ((IconButtonContent)owner).UpdateGap()));
    private readonly IconElement icon = new() { Decorative = true, VerticalAlignment = VerticalAlignment.Center };
    private readonly TextBlock label = new() { TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };

    internal static Style CreateStyle(IconTokens tokens)
    {
        var style = new Style(typeof(IconButtonContent));
        style.Setters.Add(new Setter(labelGapProperty, tokens.LabelGap));
        return style;
    }

    private void UpdateGap()
    {
        var margin = new Thickness(0, 0, string.IsNullOrEmpty(label.Text) ? 0 : (double)GetValue(labelGapProperty), 0);
        if (icon.Margin != margin) icon.Margin = margin;
    }

    internal IconButtonContent()
    {
        IsHitTestVisible = false;
        SetDock(icon, Dock.Left);
        Children.Add(icon);
        Children.Add(label);
    }

    internal static void Patch(Button button, View view)
    {
        if (view.Icon is null)
        {
            button.Content = view.Content;
            button.ClearValue(ToolTipProperty);
            return;
        }
        var content = button.Content as IconButtonContent;
        if (content is null) button.Content = content = new IconButtonContent();
        content.icon.Update(view.Icon, view.TextSize);
        var hasLabel = !string.IsNullOrEmpty(view.Content);
        if (content.label.Text != view.Content) content.label.Text = view.Content;
        content.UpdateGap();
        var visibility = hasLabel ? Visibility.Visible : Visibility.Collapsed;
        if (content.label.Visibility != visibility) content.label.Visibility = visibility;
        var tooltip = hasLabel ? null : view.AccessibleName;
        if (!Equals(button.ToolTip, tooltip)) button.ToolTip = tooltip;
    }
}
