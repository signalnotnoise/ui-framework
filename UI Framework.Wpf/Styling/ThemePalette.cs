using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using UI_Framework;

namespace UI_Framework.Wpf;

/// <summary>Dispatcher-owned brushes; system events retain only weak palette references.</summary>
internal sealed class ThemePalette
{
    private static readonly object Gate = new();
    private static readonly List<WeakReference<ThemePalette>> Palettes = [];
    internal static readonly object ResourceKey = new();
    private readonly WeakReference<FrameworkElement> root;
    private readonly Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
    private readonly Dictionary<string, (SolidColorBrush Brush, Color Normal, ThemeColor Source)> colors = [];

    static ThemePalette()
    {
        SystemParameters.StaticPropertyChanged += (_, _) => RefreshAll();
        SystemEvents.UserPreferenceChanged += (_, _) => RefreshAll();
    }

    internal ThemePalette(ThemeTokens theme, FrameworkElement root)
    {
        this.root = new(root);
        foreach (var property in typeof(ThemeTokens).GetProperties())
            if (property.PropertyType == typeof(string))
            {
                var color = (Color)ColorConverter.ConvertFromString((string)property.GetValue(theme)!);
                var source = new ThemeColor(color);
                var brush = new SolidColorBrush();
                System.Windows.Data.BindingOperations.SetBinding(brush, SolidColorBrush.ColorProperty,
                    new System.Windows.Data.Binding(nameof(ThemeColor.Value)) { Source = source });
                colors.Add(property.Name, (brush, color, source));
            }
        lock (Gate)
        {
            Palettes.RemoveAll(reference => !reference.TryGetTarget(out _));
            Palettes.Add(new(this));
        }
    }

    internal SolidColorBrush this[string role] => colors[role].Brush;

    internal void Refresh(bool contrast)
    {
        dispatcher.VerifyAccess();
        if (!root.TryGetTarget(out var scope) || !ReferenceEquals(scope.Resources[ResourceKey], this)) return;
        scope.SetValue(TextBoxStyles.ContrastProperty, contrast);
        foreach (var (role, entry) in colors)
        {
            var color = !contrast ? entry.Normal : role switch
            {
                nameof(ThemeTokens.Ink) or nameof(ThemeTokens.Border) => SystemColors.WindowTextColor,
                nameof(ThemeTokens.Muted) => SystemColors.GrayTextColor,
                nameof(ThemeTokens.Focus) or nameof(ThemeTokens.Accent) or nameof(ThemeTokens.AccentHover) or nameof(ThemeTokens.AccentPressed) => SystemColors.HighlightColor,
                nameof(ThemeTokens.OnAccent) => SystemColors.HighlightTextColor,
                _ => SystemColors.WindowColor
            };
            entry.Source.Value = color;
        }
    }

    private static void RefreshAll()
    {
        lock (Gate)
        {
            Palettes.RemoveAll(reference => !reference.TryGetTarget(out _));
            foreach (var reference in Palettes)
                if (reference.TryGetTarget(out var palette) && !palette.dispatcher.HasShutdownStarted)
                    palette.dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
                    {
                        if (reference.TryGetTarget(out var current)) current.Refresh(SystemParameters.HighContrast);
                    }));
        }
    }
}
