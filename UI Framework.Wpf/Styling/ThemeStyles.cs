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
        var resources = root.Resources;
        var ownsInitialization = false;
        try
        {
            resources.BeginInit();
            ownsInitialization = true;
        }
        catch (InvalidOperationException)
        {
            // WPF rejects nested BeginInit. Join the caller's existing update;
            // only the caller may end that initialization scope.
        }
        try
        {
            var palette = new ThemePalette(theme, root);
            resources[ThemePalette.ResourceKey] = palette;
            palette.Refresh(SystemParameters.HighContrast);
            resources[typeof(Button)] = ButtonStyle(theme, palette);
            resources[typeof(TextBox)] = TextBoxStyles.Create(theme);
            resources[typeof(PasswordBox)] = InputStyle(typeof(PasswordBox), theme, palette);
            resources[typeof(ComboBox)] = PickerStyles.Create(BaseStyle(typeof(ComboBox), theme, palette), theme, palette);
            resources[typeof(CheckBox)] = ToggleStyles.Create(theme, palette);
        }
        finally
        {
            if (ownsInitialization) resources.EndInit();
        }
    }



    private static FrameworkElementFactory Border(ThemeTokens theme, ThemePalette palette)
    {
        var border = new FrameworkElementFactory(typeof(Border), "Chrome");
        border.SetValue(System.Windows.Controls.Border.CornerRadiusProperty, new CornerRadius(theme.ControlRadius));
        border.SetValue(System.Windows.Controls.Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        border.SetValue(System.Windows.Controls.Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
        border.SetValue(System.Windows.Controls.Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
        border.SetValue(System.Windows.Controls.Border.PaddingProperty, new Thickness(theme.ControlPadding));
        return border;
    }

    private static Style BaseStyle(Type type, ThemeTokens theme, ThemePalette palette, double minimumHeight = 36)
    {
        var style = new Style(type);
        style.Setters.Add(new Setter(Control.BackgroundProperty, palette[nameof(ThemeTokens.Surface)]));
        style.Setters.Add(new Setter(Control.ForegroundProperty, palette[nameof(ThemeTokens.Ink)]));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, palette[nameof(ThemeTokens.Border)]));
        style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(theme.ControlPadding)));
        style.Setters.Add(new Setter(FrameworkElement.MinHeightProperty, minimumHeight));
        return style;
    }

    private static void State(ControlTemplate template, DependencyProperty property, object value, DependencyProperty target, object setting)
    {
        var trigger = new Trigger { Property = property, Value = value };
        trigger.Setters.Add(new Setter(target, setting));
        template.Triggers.Add(trigger);
    }

    private static Style ButtonStyle(ThemeTokens theme, ThemePalette palette)
    {
        var style = BaseStyle(typeof(Button), theme, palette, minimumHeight: 0);
        // Keep the usual minimum for automatic sizing, but let a native explicit
        // Height define compact chrome. A local MinHeight still takes precedence.
        var automaticHeight = new Trigger { Property = FrameworkElement.HeightProperty, Value = double.NaN };
        automaticHeight.Setters.Add(new Setter(FrameworkElement.MinHeightProperty, 36.0));
        style.Triggers.Add(automaticHeight);
        var template = new ControlTemplate(typeof(Button));
        var border = Border(theme, palette);
        border.SetValue(System.Windows.Controls.Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));
        var content = new FrameworkElementFactory(typeof(ButtonContentPresenter), "ButtonContent");
        content.SetValue(ContentPresenter.ContentProperty, new TemplateBindingExtension(ContentControl.ContentProperty));
        content.SetValue(ContentPresenter.ContentTemplateProperty, new TemplateBindingExtension(ContentControl.ContentTemplateProperty));
        content.SetValue(ContentPresenter.ContentTemplateSelectorProperty, new TemplateBindingExtension(ContentControl.ContentTemplateSelectorProperty));
        content.SetValue(ContentPresenter.ContentStringFormatProperty, new TemplateBindingExtension(ContentControl.ContentStringFormatProperty));
        content.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        content.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        border.AppendChild(content);
        template.VisualTree = border;
        var quiet = new Trigger { Property = AppearanceProperty, Value = ButtonStyleKind.Quiet };
        quiet.Setters.Add(new Setter(Control.BorderBrushProperty, Brushes.Transparent));
        quiet.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
        template.Triggers.Add(quiet);
        State(template, UIElement.IsMouseOverProperty, true, Control.BackgroundProperty, palette[nameof(ThemeTokens.Hover)]);
        State(template, System.Windows.Controls.Primitives.ButtonBase.IsPressedProperty, true, Control.BackgroundProperty, palette[nameof(ThemeTokens.Pressed)]);
        var primary = new Trigger { Property = AppearanceProperty, Value = ButtonStyleKind.Primary };
        primary.Setters.Add(new Setter(Control.BackgroundProperty, palette[nameof(ThemeTokens.Accent)]));
        primary.Setters.Add(new Setter(Control.ForegroundProperty, palette[nameof(ThemeTokens.OnAccent)]));
        primary.Setters.Add(new Setter(Control.BorderBrushProperty, palette[nameof(ThemeTokens.Accent)]));
        template.Triggers.Add(primary);
        foreach (var (property, color) in new[] { (UIElement.IsMouseOverProperty, nameof(ThemeTokens.AccentHover)), (System.Windows.Controls.Primitives.ButtonBase.IsPressedProperty, nameof(ThemeTokens.AccentPressed)) })
        {
            var trigger = new MultiTrigger();
            trigger.Conditions.Add(new Condition(AppearanceProperty, ButtonStyleKind.Primary));
            trigger.Conditions.Add(new Condition(property, true));
            trigger.Setters.Add(new Setter(Control.BackgroundProperty, palette[color]));
            template.Triggers.Add(trigger);
        }
        State(template, UIElement.IsKeyboardFocusedProperty, true, Control.BorderBrushProperty, palette[nameof(ThemeTokens.Focus)]);
        State(template, UIElement.IsEnabledProperty, false, Control.ForegroundProperty, palette[nameof(ThemeTokens.Muted)]);
        State(template, UIElement.IsEnabledProperty, false, Control.BackgroundProperty, palette[nameof(ThemeTokens.Surface)]);
        style.Setters.Add(new Setter(Control.TemplateProperty, template));
        return style;
    }

    private static Style InputStyle(Type type, ThemeTokens theme, ThemePalette palette)
    {
        var style = BaseStyle(type, theme, palette);
        var template = new ControlTemplate(type);
        var border = Border(theme, palette);
        // TextBox/PasswordBox already apply their Padding to the native editing
        // content. Applying it again on the border clips compact query fields.
        border.SetValue(System.Windows.Controls.Border.PaddingProperty, new Thickness(0));
        border.AppendChild(new FrameworkElementFactory(typeof(ScrollViewer), "PART_ContentHost"));
        template.VisualTree = border;
        State(template, UIElement.IsMouseOverProperty, true, Control.BorderBrushProperty, palette[nameof(ThemeTokens.Muted)]);
        State(template, UIElement.IsKeyboardFocusWithinProperty, true, Control.BorderBrushProperty, palette[nameof(ThemeTokens.Focus)]);
        State(template, UIElement.IsEnabledProperty, false, Control.ForegroundProperty, palette[nameof(ThemeTokens.Muted)]);
        State(template, UIElement.IsEnabledProperty, false, Control.BackgroundProperty, palette[nameof(ThemeTokens.Surface)]);
        style.Setters.Add(new Setter(Control.TemplateProperty, template));
        return style;
    }
}
