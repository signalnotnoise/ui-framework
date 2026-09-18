using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UI_Framework;

namespace UI_Framework.Wpf;

/// <summary>Installs scoped native styles without changing application-wide resources.</summary>
public static class ThemeStyles
{
    public static readonly DependencyProperty AppearanceProperty = DependencyProperty.RegisterAttached(
        "Appearance", typeof(ButtonStyleKind), typeof(ThemeStyles), new PropertyMetadata(ButtonStyleKind.Secondary));
    public static void SetAppearance(DependencyObject target, ButtonStyleKind value) => target.SetValue(AppearanceProperty, value);
    public static ButtonStyleKind GetAppearance(DependencyObject target) => (ButtonStyleKind)target.GetValue(AppearanceProperty);

    public static void Apply(FrameworkElement root, ThemeTokens theme)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(theme);
        if (!double.IsFinite(theme.ControlRadius) || theme.ControlRadius < 0 || !double.IsFinite(theme.ControlPadding) || theme.ControlPadding < 0)
            throw new ArgumentOutOfRangeException(nameof(theme));
        root.Resources[typeof(Button)] = ButtonStyle(theme);
        root.Resources[typeof(TextBox)] = InputStyle(theme);
    }

    private static SolidColorBrush Brush(string color)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        brush.Freeze();
        return brush;
    }

    private static FrameworkElementFactory Border(ThemeTokens theme)
    {
        var border = new FrameworkElementFactory(typeof(Border), "Chrome");
        border.SetValue(System.Windows.Controls.Border.CornerRadiusProperty, new CornerRadius(theme.ControlRadius));
        border.SetValue(System.Windows.Controls.Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        border.SetValue(System.Windows.Controls.Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
        border.SetValue(System.Windows.Controls.Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
        border.SetValue(System.Windows.Controls.Border.PaddingProperty, new Thickness(theme.ControlPadding));
        return border;
    }

    private static Style BaseStyle(Type type, ThemeTokens theme)
    {
        var style = new Style(type);
        style.Setters.Add(new Setter(Control.BackgroundProperty, Brush(theme.Surface)));
        style.Setters.Add(new Setter(Control.ForegroundProperty, Brush(theme.Ink)));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, Brush(theme.Border)));
        style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(theme.ControlPadding)));
        style.Setters.Add(new Setter(FrameworkElement.MinHeightProperty, 36.0));
        return style;
    }

    private static void State(ControlTemplate template, DependencyProperty property, object value, DependencyProperty target, object setting)
    {
        var trigger = new Trigger { Property = property, Value = value };
        trigger.Setters.Add(new Setter(target, setting));
        template.Triggers.Add(trigger);
    }

    private static Style ButtonStyle(ThemeTokens theme)
    {
        var style = BaseStyle(typeof(Button), theme);
        var template = new ControlTemplate(typeof(Button));
        var border = Border(theme);
        var content = new FrameworkElementFactory(typeof(TextBlock));
        content.SetValue(TextBlock.TextProperty, new TemplateBindingExtension(ContentControl.ContentProperty));
        content.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
        content.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        content.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        border.AppendChild(content);
        template.VisualTree = border;
        var quiet = new Trigger { Property = AppearanceProperty, Value = ButtonStyleKind.Quiet };
        quiet.Setters.Add(new Setter(Control.BorderBrushProperty, Brushes.Transparent));
        quiet.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
        template.Triggers.Add(quiet);
        State(template, UIElement.IsMouseOverProperty, true, Control.BackgroundProperty, Brush(theme.Hover));
        State(template, System.Windows.Controls.Primitives.ButtonBase.IsPressedProperty, true, Control.BackgroundProperty, Brush(theme.Pressed));
        var primary = new Trigger { Property = AppearanceProperty, Value = ButtonStyleKind.Primary };
        primary.Setters.Add(new Setter(Control.BackgroundProperty, Brush(theme.Accent)));
        primary.Setters.Add(new Setter(Control.ForegroundProperty, Brush(theme.OnAccent)));
        primary.Setters.Add(new Setter(Control.BorderBrushProperty, Brush(theme.Accent)));
        template.Triggers.Add(primary);
        foreach (var (property, color) in new[] { (UIElement.IsMouseOverProperty, theme.AccentHover), (System.Windows.Controls.Primitives.ButtonBase.IsPressedProperty, theme.AccentPressed) })
        {
            var trigger = new MultiTrigger();
            trigger.Conditions.Add(new Condition(AppearanceProperty, ButtonStyleKind.Primary));
            trigger.Conditions.Add(new Condition(property, true));
            trigger.Setters.Add(new Setter(Control.BackgroundProperty, Brush(color)));
            template.Triggers.Add(trigger);
        }
        State(template, UIElement.IsKeyboardFocusedProperty, true, Control.BorderBrushProperty, Brush(theme.Focus));
        State(template, UIElement.IsEnabledProperty, false, UIElement.OpacityProperty, 0.45);
        style.Setters.Add(new Setter(Control.TemplateProperty, template));
        return style;
    }

    private static Style InputStyle(ThemeTokens theme)
    {
        var style = BaseStyle(typeof(TextBox), theme);
        var template = new ControlTemplate(typeof(TextBox));
        var border = Border(theme);
        border.AppendChild(new FrameworkElementFactory(typeof(ScrollViewer), "PART_ContentHost"));
        template.VisualTree = border;
        State(template, UIElement.IsMouseOverProperty, true, Control.BorderBrushProperty, Brush(theme.Muted));
        State(template, UIElement.IsKeyboardFocusWithinProperty, true, Control.BorderBrushProperty, Brush(theme.Focus));
        State(template, UIElement.IsEnabledProperty, false, UIElement.OpacityProperty, 0.45);
        style.Setters.Add(new Setter(Control.TemplateProperty, template));
        return style;
    }
}
