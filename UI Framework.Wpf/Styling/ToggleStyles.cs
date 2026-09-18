using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;
using UI_Framework;
using Path = System.Windows.Shapes.Path;

namespace UI_Framework.Wpf;

/// <summary>Scoped checkbox visuals; native CheckBox retains input and automation behavior.</summary>
internal static class ToggleStyles
{
    internal static Style Create(ThemeTokens theme)
    {
        var style = new Style(typeof(CheckBox));
        style.Setters.Add(new Setter(Control.ForegroundProperty, Brush(theme.Ink)));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(4)));
        // The template supplies a theme-colored focus outline instead of system black.
        style.Setters.Add(new Setter(Control.FocusVisualStyleProperty, null));
        var template = new ControlTemplate(typeof(CheckBox));
        var focus = new FrameworkElementFactory(typeof(Border), "FocusChrome");
        focus.SetValue(Border.BorderBrushProperty, Brushes.Transparent);
        focus.SetValue(Border.BorderThicknessProperty, new Thickness(2));
        focus.SetValue(Border.CornerRadiusProperty, new CornerRadius(theme.ControlRadius));
        focus.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));
        focus.SetValue(Border.BackgroundProperty, Brushes.Transparent);
        var row = new FrameworkElementFactory(typeof(DockPanel));
        var indicator = new FrameworkElementFactory(typeof(Border), "Indicator");
        indicator.SetValue(DockPanel.DockProperty, Dock.Left);
        indicator.SetValue(FrameworkElement.WidthProperty, 20.0);
        indicator.SetValue(FrameworkElement.HeightProperty, 20.0);
        indicator.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 8, 0));
        indicator.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        indicator.SetValue(Border.CornerRadiusProperty, new CornerRadius(3));
        indicator.SetValue(Border.BorderThicknessProperty, new Thickness(1));
        indicator.SetValue(Border.BorderBrushProperty, Brush(theme.Border));
        indicator.SetValue(Border.BackgroundProperty, Brush(theme.Surface));
        var mark = new FrameworkElementFactory(typeof(Path), "CheckMark");
        mark.SetValue(Path.DataProperty, Geometry.Parse("M 3,8 L 7,12 L 15,4"));
        mark.SetValue(Shape.StrokeProperty, Brush(theme.OnAccent));
        mark.SetValue(Shape.StrokeThicknessProperty, 2.0);
        mark.SetValue(Shape.StrokeStartLineCapProperty, PenLineCap.Round);
        mark.SetValue(Shape.StrokeEndLineCapProperty, PenLineCap.Round);
        mark.SetValue(UIElement.VisibilityProperty, Visibility.Hidden);
        indicator.AppendChild(mark);
        row.AppendChild(indicator);
        var label = new FrameworkElementFactory(typeof(TextBlock), "Label");
        label.SetValue(TextBlock.TextProperty, new TemplateBindingExtension(ContentControl.ContentProperty));
        label.SetValue(TextBlock.ForegroundProperty, new TemplateBindingExtension(Control.ForegroundProperty));
        label.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
        label.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        row.AppendChild(label);
        focus.AppendChild(row);
        template.VisualTree = focus;
        State(template, UIElement.IsMouseOverProperty, true, "Indicator", Border.BackgroundProperty, Brush(theme.Hover));
        State(template, ButtonBase.IsPressedProperty, true, "Indicator", Border.BackgroundProperty, Brush(theme.Pressed));
        State(template, ToggleButton.IsCheckedProperty, true, "Indicator", Border.BackgroundProperty, Brush(theme.Accent));
        State(template, ToggleButton.IsCheckedProperty, true, "CheckMark", UIElement.VisibilityProperty, Visibility.Visible);
        foreach (var (property, color) in new[] { (UIElement.IsMouseOverProperty, theme.AccentHover), (ButtonBase.IsPressedProperty, theme.AccentPressed) })
        {
            var state = new MultiTrigger();
            state.Conditions.Add(new Condition(ToggleButton.IsCheckedProperty, true));
            state.Conditions.Add(new Condition(property, true));
            state.Setters.Add(new Setter(Border.BackgroundProperty, Brush(color), "Indicator"));
            template.Triggers.Add(state);
        }
        State(template, UIElement.IsKeyboardFocusWithinProperty, true, "FocusChrome", Border.BorderBrushProperty, Brush(theme.Focus));
        var disabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
        disabled.Setters.Add(new Setter(Control.ForegroundProperty, Brush(theme.Muted)));
        disabled.Setters.Add(new Setter(Border.BackgroundProperty, Brush(theme.Surface), "Indicator"));
        disabled.Setters.Add(new Setter(Border.BorderBrushProperty, Brush(theme.Muted), "Indicator"));
        disabled.Setters.Add(new Setter(Shape.StrokeProperty, Brush(theme.Muted), "CheckMark"));
        template.Triggers.Add(disabled);
        style.Setters.Add(new Setter(Control.TemplateProperty, template));
        return style;
    }

    private static void State(ControlTemplate template, DependencyProperty property, object value, string targetName, DependencyProperty target, object setting)
    {
        var state = new Trigger { Property = property, Value = value };
        state.Setters.Add(new Setter(target, setting, targetName));
        template.Triggers.Add(state);
    }

    private static SolidColorBrush Brush(string color)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        brush.Freeze();
        return brush;
    }
}
