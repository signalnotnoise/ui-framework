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
    internal static Style Create(ThemeTokens theme, ThemePalette palette)
    {
        var style = new Style(typeof(CheckBox));
        style.Setters.Add(new Setter(Control.ForegroundProperty, palette[nameof(ThemeTokens.Ink)]));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(2)));
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
        indicator.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 4, 0));
        indicator.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        indicator.SetValue(Border.CornerRadiusProperty, new CornerRadius(3));
        indicator.SetValue(Border.BorderThicknessProperty, new Thickness(1));
        indicator.SetValue(Border.BorderBrushProperty, palette[nameof(ThemeTokens.Border)]);
        indicator.SetValue(Border.BackgroundProperty, palette[nameof(ThemeTokens.Surface)]);
        var mark = new FrameworkElementFactory(typeof(Path), "CheckMark");
        var checkGeometry = Geometry.Parse("M 3,8 L 7,12 L 15,4");
        checkGeometry.Freeze();
        mark.SetValue(Path.DataProperty, checkGeometry);
        mark.SetValue(Shape.StrokeProperty, palette[nameof(ThemeTokens.OnAccent)]);
        mark.SetValue(Shape.StrokeThicknessProperty, 2.0);
        mark.SetValue(Shape.StrokeStartLineCapProperty, PenLineCap.Round);
        mark.SetValue(Shape.StrokeEndLineCapProperty, PenLineCap.Round);
        // The fixed-size indicator owns layout; an unchecked mark needs no measure work.
        mark.SetValue(UIElement.VisibilityProperty, Visibility.Collapsed);
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
        State(template, UIElement.IsMouseOverProperty, true, "Indicator", Border.BackgroundProperty, palette[nameof(ThemeTokens.Hover)]);
        State(template, ButtonBase.IsPressedProperty, true, "Indicator", Border.BackgroundProperty, palette[nameof(ThemeTokens.Pressed)]);
        State(template, ToggleButton.IsCheckedProperty, true, "Indicator", Border.BackgroundProperty, palette[nameof(ThemeTokens.Accent)]);
        State(template, ToggleButton.IsCheckedProperty, true, "CheckMark", UIElement.VisibilityProperty, Visibility.Visible);
        foreach (var (property, color) in new[] { (UIElement.IsMouseOverProperty, nameof(ThemeTokens.AccentHover)), (ButtonBase.IsPressedProperty, nameof(ThemeTokens.AccentPressed)) })
        {
            var state = new MultiTrigger();
            state.Conditions.Add(new Condition(ToggleButton.IsCheckedProperty, true));
            state.Conditions.Add(new Condition(property, true));
            state.Setters.Add(new Setter(Border.BackgroundProperty, palette[color], "Indicator"));
            template.Triggers.Add(state);
        }
        State(template, UIElement.IsKeyboardFocusWithinProperty, true, "FocusChrome", Border.BorderBrushProperty, palette[nameof(ThemeTokens.Focus)]);
        var disabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
        disabled.Setters.Add(new Setter(Control.ForegroundProperty, palette[nameof(ThemeTokens.Muted)]));
        disabled.Setters.Add(new Setter(Border.BackgroundProperty, palette[nameof(ThemeTokens.Surface)], "Indicator"));
        disabled.Setters.Add(new Setter(Border.BorderBrushProperty, palette[nameof(ThemeTokens.Muted)], "Indicator"));
        disabled.Setters.Add(new Setter(Shape.StrokeProperty, palette[nameof(ThemeTokens.Muted)], "CheckMark"));
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


}
