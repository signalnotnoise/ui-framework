using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using UI_Framework;
using WpfBinding = System.Windows.Data.Binding;

namespace UI_Framework.Wpf;

/// <summary>Theme-owned visuals around the native non-editable ComboBox behavior.</summary>
internal static class PickerStyles
{
    internal static Style Create(Style style, ThemeTokens theme)
    {
        var template = new ControlTemplate(typeof(ComboBox));
        var root = new FrameworkElementFactory(typeof(Grid));
        var toggle = new FrameworkElementFactory(typeof(ToggleButton), "DropDownToggle");
        toggle.SetValue(UIElement.FocusableProperty, false);
        toggle.SetValue(ButtonBase.ClickModeProperty, ClickMode.Press);
        toggle.SetBinding(ToggleButton.IsCheckedProperty, Parent("IsDropDownOpen", BindingMode.TwoWay));
        foreach (var property in new[] { Control.BackgroundProperty, Control.BorderBrushProperty, Control.BorderThicknessProperty })
            toggle.SetValue(property, new TemplateBindingExtension(property));
        var toggleTemplate = new ControlTemplate(typeof(ToggleButton));
        var chrome = Chrome(theme, "Chrome");
        toggleTemplate.VisualTree = chrome;
        Trigger(toggleTemplate, ButtonBase.IsPressedProperty, true, Control.BackgroundProperty, Brush(theme.Pressed));
        toggle.SetValue(Control.TemplateProperty, toggleTemplate);
        root.AppendChild(toggle);

        // Presentation sits over the toggle and leaves all pointer handling to it.
        var display = new FrameworkElementFactory(typeof(DockPanel));
        display.SetValue(UIElement.IsHitTestVisibleProperty, false);
        display.SetValue(FrameworkElement.MarginProperty, new TemplateBindingExtension(Control.PaddingProperty));
        var arrow = new FrameworkElementFactory(typeof(TextBlock));
        arrow.SetValue(DockPanel.DockProperty, Dock.Right);
        arrow.SetValue(TextBlock.TextProperty, "\u25be");
        arrow.SetValue(FrameworkElement.MarginProperty, new Thickness(10, 0, 0, 0));
        arrow.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        arrow.SetValue(TextBlock.ForegroundProperty, new TemplateBindingExtension(Control.ForegroundProperty));
        display.AppendChild(arrow);
        var selection = new FrameworkElementFactory(typeof(ContentPresenter), "SelectionPresenter");
        selection.SetValue(ContentPresenter.ContentProperty, new TemplateBindingExtension(ComboBox.SelectionBoxItemProperty));
        selection.SetValue(ContentPresenter.ContentTemplateProperty, new TemplateBindingExtension(ComboBox.SelectionBoxItemTemplateProperty));
        selection.SetValue(ContentPresenter.ContentStringFormatProperty, new TemplateBindingExtension(ComboBox.SelectionBoxItemStringFormatProperty));
        selection.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        display.AppendChild(selection);
        root.AppendChild(display);

        // Keep the required popup part and ItemsPresenter so ComboBox continues to
        // own selection, capture, keyboard navigation, type-ahead and dismissal.
        var popup = new FrameworkElementFactory(typeof(Popup), "PART_Popup");
        popup.SetValue(Popup.PlacementProperty, PlacementMode.Bottom);
        popup.SetValue(Popup.AllowsTransparencyProperty, true);
        popup.SetValue(UIElement.FocusableProperty, false);
        popup.SetBinding(Popup.PlacementTargetProperty, Parent(""));
        popup.SetBinding(Popup.IsOpenProperty, Parent("IsDropDownOpen"));
        var dropDown = new FrameworkElementFactory(typeof(Border), "DropDownChrome");
        dropDown.SetValue(Border.BackgroundProperty, Brush(theme.Surface));
        dropDown.SetValue(Border.BorderBrushProperty, Brush(theme.Border));
        dropDown.SetValue(Border.BorderThicknessProperty, new Thickness(1));
        dropDown.SetValue(Border.CornerRadiusProperty, new CornerRadius(theme.ControlRadius));
        dropDown.SetBinding(FrameworkElement.MinWidthProperty, Parent("ActualWidth"));
        dropDown.SetBinding(FrameworkElement.MaxHeightProperty, Parent("MaxDropDownHeight"));
        var scroll = new FrameworkElementFactory(typeof(ScrollViewer));
        scroll.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
        scroll.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
        scroll.SetValue(ScrollViewer.CanContentScrollProperty, true);
        scroll.AppendChild(new FrameworkElementFactory(typeof(ItemsPresenter), "ItemsPresenter"));
        dropDown.AppendChild(scroll);
        popup.AppendChild(dropDown);
        root.AppendChild(popup);
        template.VisualTree = root;
        Trigger(template, UIElement.IsMouseOverProperty, true, Control.BackgroundProperty, Brush(theme.Hover));
        Trigger(template, ComboBox.IsDropDownOpenProperty, true, Control.BackgroundProperty, Brush(theme.Pressed));
        Trigger(template, UIElement.IsKeyboardFocusWithinProperty, true, Control.BorderBrushProperty, Brush(theme.Focus));
        Trigger(template, UIElement.IsEnabledProperty, false, UIElement.OpacityProperty, 0.45);
        style.Setters.Add(new Setter(Control.TemplateProperty, template));
        style.Setters.Add(new Setter(ItemsControl.ItemContainerStyleProperty, ItemStyle(theme)));
        return style;
    }

    private static Style ItemStyle(ThemeTokens theme)
    {
        var style = new Style(typeof(ComboBoxItem));
        style.Setters.Add(new Setter(Control.BackgroundProperty, Brush(theme.Surface)));
        style.Setters.Add(new Setter(Control.ForegroundProperty, Brush(theme.Ink)));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, Brushes.Transparent));
        style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(theme.ControlPadding)));
        style.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        var template = new ControlTemplate(typeof(ComboBoxItem));
        var chrome = Chrome(theme, "ItemChrome");
        chrome.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));
        var content = new FrameworkElementFactory(typeof(ContentPresenter));
        content.SetValue(ContentPresenter.ContentSourceProperty, "Content");
        chrome.AppendChild(content);
        template.VisualTree = chrome;
        Trigger(template, ComboBoxItem.IsHighlightedProperty, true, Control.BackgroundProperty, Brush(theme.Hover));
        Trigger(template, ComboBoxItem.IsSelectedProperty, true, Control.BackgroundProperty, Brush(theme.Accent));
        Trigger(template, ComboBoxItem.IsSelectedProperty, true, Control.ForegroundProperty, Brush(theme.OnAccent));
        var selectedHighlight = new MultiTrigger();
        selectedHighlight.Conditions.Add(new Condition(ComboBoxItem.IsSelectedProperty, true));
        selectedHighlight.Conditions.Add(new Condition(ComboBoxItem.IsHighlightedProperty, true));
        selectedHighlight.Setters.Add(new Setter(Control.BackgroundProperty, Brush(theme.AccentHover)));
        template.Triggers.Add(selectedHighlight);
        Trigger(template, UIElement.IsKeyboardFocusWithinProperty, true, Control.BorderBrushProperty, Brush(theme.Focus));
        Trigger(template, UIElement.IsEnabledProperty, false, UIElement.OpacityProperty, 0.45);
        style.Setters.Add(new Setter(Control.TemplateProperty, template));
        return style;
    }

    private static FrameworkElementFactory Chrome(ThemeTokens theme, string name)
    {
        var border = new FrameworkElementFactory(typeof(Border), name);
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(theme.ControlRadius));
        border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
        border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
        return border;
    }

    private static WpfBinding Parent(string path, BindingMode mode = BindingMode.OneWay) =>
        new(path) { RelativeSource = RelativeSource.TemplatedParent, Mode = mode };

    private static void Trigger(ControlTemplate template, DependencyProperty property, object value, DependencyProperty target, object setting)
    {
        var trigger = new Trigger { Property = property, Value = value };
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
