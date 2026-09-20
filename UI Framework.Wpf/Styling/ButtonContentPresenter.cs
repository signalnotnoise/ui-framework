using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace UI_Framework.Wpf;

internal sealed class ButtonContentPresenter : ContentPresenter
{
    private static readonly DataTemplateKey StringTemplateKey = new(typeof(string));
    public ButtonContentPresenter() { }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        // Preserve wrapping for ordinary labels without changing application-owned visuals
        // or explicitly/implicitly selected data templates.
        if (Content is string && ContentTemplate is null && ContentTemplateSelector is null
            && TryFindResource(StringTemplateKey) is null
            && VisualTreeHelper.GetChildrenCount(this) == 1
            && VisualTreeHelper.GetChild(this, 0) is TextBlock text)
            text.SetCurrentValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
    }
}
