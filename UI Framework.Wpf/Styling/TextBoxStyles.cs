using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using UI_Framework;

namespace UI_Framework.Wpf;

/// <summary>Frozen normal colors; system resource expressions are active only in contrast mode.</summary>
internal static class TextBoxStyles
{
    internal static readonly DependencyProperty ContrastProperty = DependencyProperty.RegisterAttached(
        "Contrast", typeof(bool), typeof(TextBoxStyles), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

    internal static Style Create(ThemeTokens theme)
    {
        var surface = Brush(theme.Surface);
        var ink = Brush(theme.Ink);
        var borderBrush = Brush(theme.Border);
        var muted = Brush(theme.Muted);
        var focus = Brush(theme.Focus);
        var style = new Style(typeof(TextBox));
        style.Setters.Add(new Setter(Control.BackgroundProperty, surface));
        style.Setters.Add(new Setter(Control.ForegroundProperty, ink));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, borderBrush));
        style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(theme.ControlPadding)));
        style.Setters.Add(new Setter(FrameworkElement.MinHeightProperty, 36.0));
        var template = new ControlTemplate(typeof(TextBox));
        var border = new FrameworkElementFactory(typeof(Border), "Chrome");
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(theme.ControlRadius));
        border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
        border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
        border.AppendChild(new FrameworkElementFactory(typeof(ScrollViewer), "PART_ContentHost"));
        template.VisualTree = border;
        Add(template, UIElement.IsMouseOverProperty, true, Control.BorderBrushProperty, muted);
        Add(template, UIElement.IsKeyboardFocusWithinProperty, true, Control.BorderBrushProperty, focus);
        Add(template, UIElement.IsEnabledProperty, false, Control.ForegroundProperty, muted);
        Add(template, UIElement.IsEnabledProperty, false, Control.BackgroundProperty, surface);
        var contrast = new Trigger { Property = ContrastProperty, Value = true };
        contrast.Setters.Add(SystemColor(Control.BackgroundProperty, SystemColors.WindowBrushKey));
        contrast.Setters.Add(SystemColor(Control.ForegroundProperty, SystemColors.WindowTextBrushKey));
        contrast.Setters.Add(SystemColor(Control.BorderBrushProperty, SystemColors.WindowTextBrushKey));
        contrast.Setters.Add(SystemColor(TextBoxBase.SelectionBrushProperty, SystemColors.HighlightBrushKey));
        contrast.Setters.Add(SystemColor(TextBoxBase.SelectionTextBrushProperty, SystemColors.HighlightTextBrushKey));
        contrast.Setters.Add(SystemColor(TextBoxBase.CaretBrushProperty, SystemColors.WindowTextBrushKey));
        contrast.Setters.Add(new Setter(TextBoxBase.SelectionOpacityProperty, 1.0));
        template.Triggers.Add(contrast);
        AddContrastState(template, UIElement.IsMouseOverProperty, true, Control.BorderBrushProperty, SystemColors.GrayTextBrushKey);
        AddContrastState(template, UIElement.IsKeyboardFocusWithinProperty, true, Control.BorderBrushProperty, SystemColors.HighlightBrushKey);
        AddContrastState(template, UIElement.IsEnabledProperty, false, Control.ForegroundProperty, SystemColors.GrayTextBrushKey);
        style.Setters.Add(new Setter(Control.TemplateProperty, template));
        return style;
    }

    private static Setter SystemColor(DependencyProperty property, ResourceKey key) => new(property, new DynamicResourceExtension(key));

    private static void AddContrastState(ControlTemplate template, DependencyProperty state, bool value, DependencyProperty target, ResourceKey key)
    {
        var trigger = new MultiTrigger();
        trigger.Conditions.Add(new Condition(ContrastProperty, true));
        trigger.Conditions.Add(new Condition(state, value));
        trigger.Setters.Add(SystemColor(target, key));
        template.Triggers.Add(trigger);
    }

    private static void Add(ControlTemplate template, DependencyProperty state, bool value, DependencyProperty target, object setting)
    {
        var trigger = new Trigger { Property = state, Value = value };
        trigger.Setters.Add(new Setter(target, setting));
        template.Triggers.Add(trigger);
    }

    private static SolidColorBrush Brush(string color)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        brush.Freeze();
        return brush;
    }
}
