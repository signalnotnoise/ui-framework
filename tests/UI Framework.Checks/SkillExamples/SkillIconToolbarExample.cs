using System.Windows;
using System.Windows.Controls;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

public sealed class SkillIconToolbarExample
{
    // Application-owned hit target, independent of the icon's display size.
    public const double TargetSize = 32;
    private static readonly VectorIcon search = new("M10,3 A7,7 0 1 0 10,17 A7,7 0 1 0 10,3 M15,15 L21,21", strokeWidth: 2);
    private ThemeTokens theme;
    private readonly Action searchAction;

    public SkillIconToolbarExample(ThemeTokens theme, Action searchAction)
    {
        this.theme = theme;
        this.searchAction = searchAction;
    }

    public View Body() => HStack(CreateAction("Search", true), CreateAction("Unavailable search", false))
        .Spacing(theme.Spacing.Small).Padding(theme.Spacing.Small)
        .Background(theme.Canvas).Foreground(theme.Ink);

    private View CreateAction(string name, bool enabled) => IconButton(search, name, searchAction)
        .FontSize(theme.Icons.Small).Width(TargetSize).Height(TargetSize)
        .ButtonStyle(ButtonStyleKind.Quiet).IsEnabled(enabled);

    // The application's theme owner calls this with SystemParameters.HighContrast,
    // initially and again when its theme/contrast settings change.
    public void ApplyTheme(ViewHost toolbar, ThemeTokens value, bool highContrast)
    {
        theme = highContrast
            ? value with { Canvas = SystemColors.WindowColor.ToString(), Ink = SystemColors.WindowTextColor.ToString() }
            : value;
        ThemeStyles.Apply(toolbar, theme);
        var style = new Style(typeof(Button), (Style)toolbar.Resources[typeof(Button)]);
        style.Setters.Add(new Setter(FrameworkElement.WidthProperty, TargetSize));
        style.Setters.Add(new Setter(FrameworkElement.HeightProperty, TargetSize));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        // Only this dedicated toolbar opts into style-owned padding. Node sets
        // local padding; a Setter alone cannot override that value.
        style.Setters.Add(new EventSetter(FrameworkElement.LoadedEvent,
            new RoutedEventHandler(UseToolbarPadding)));
        toolbar.Resources[typeof(Button)] = style;
        toolbar.Refresh();
    }

    private static void UseToolbarPadding(object sender, RoutedEventArgs args) =>
        ((Button)sender).ClearValue(Control.PaddingProperty);
}
