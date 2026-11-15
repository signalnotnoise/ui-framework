using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

namespace SkillPreview;

internal static class AppearanceAcceptance
{
    public static void Run(string directory)
    {
        var output = Path.GetFullPath(directory);
        if (Directory.Exists(output)) throw new InvalidOperationException("Use a fresh evidence directory.");
        Directory.CreateDirectory(output);
        var app = new Application();
        using var cleanup = new WpfComCleanupPolicy(app.Dispatcher, error => throw new InvalidOperationException("Cleanup failed", error));
        var captures = new List<object>();
        var checks = 0;
        void Check(bool passed, string message) { if (!passed) throw new InvalidOperationException(message); checks++; }
        void Flush() => app.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        var light = new ThemeTokens();
        var dark = new ThemeTokens { Canvas = "#151B23", Surface = "#202A36", Ink = "#F0F4F8", Muted = "#B3C1D1",
            Accent = "#8ADCB9", AccentHover = "#AFEBD2", AccentPressed = "#65C39E", OnAccent = "#13251D",
            Hover = "#2C3A49", Pressed = "#36495B", Border = "#607287", Focus = "#92C6FF" };
        foreach (var width in new[] { 1040, 760 })
        {
            var theme = light;
            using var host = new ViewHost(() => Component<AppearanceScreen>(screen => screen.Theme = theme));
            host.FontFamily = new FontFamily("Segoe UI");
            host.FontSize = 14;
            using var source = new HwndSource(new HwndSourceParameters("FW-017 appearance acceptance")
                { Width = width, Height = 820, PositionX = -10000, PositionY = -10000, WindowStyle = unchecked((int)0x80000000) });
            source.RootVisual = host;
            ThemeStyles.Apply(host, theme);
            Flush(); Layout(host, width);
            var editor = Find<TextBox>(host).Single();
            var save = Find<Button>(host).Single(b => Equals(b.Content, "Save draft"));
            editor.Text = "Autumn release"; Flush();
            save.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent)); Flush();
            Check(Find<TextBlock>(host).Any(t => t.Text == "Saved: Autumn release"), "Valid input must clear the error.");
            Check(AutomationProperties.GetName(editor).Contains("Saved: Autumn release"), "Accessible field description must follow validation.");
            editor.Text = ""; Flush(); save.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent)); Flush();
            Check(Find<TextBlock>(host).Any(t => t.Text.StartsWith("Error: Enter")), "Invalid input must show explicit error text.");
            foreach (var mode in new[] { "light", "dark" })
            {
                theme = mode == "light" ? light : dark;
                ThemeStyles.Apply(host, theme); host.Refresh(); Flush(); Layout(host, width);
                Check(ReferenceEquals(editor, Find<TextBox>(host).Single()), "Theme replacement must retain editor identity.");
                Check(!Find<Button>(host).Single(b => Equals(b.Content, "Publish")).IsEnabled, "Publish must remain disabled.");
                foreach (var state in new[] { "normal", "focus" })
                {
                    // Like ToggleStyleTests, drive the template state on a hidden source;
                    // this is a render check, never a desktop keyboard-focus claim.
                    var key = (DependencyPropertyKey)typeof(UIElement).GetField("IsKeyboardFocusWithinPropertyKey",
                        BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
                    editor.SetValue(key, state == "focus");
                    host.UpdateLayout();
                    var chrome = (Border)editor.Template.FindName("Chrome", editor);
                    var expected = (Color)ColorConverter.ConvertFromString(state == "focus" ? theme.Focus : theme.Border);
                    Check(((SolidColorBrush)chrome.BorderBrush).Color == expected, "Editor border must reflect focus state.");
                    foreach (var text in Find<TextBlock>(host).Where(t => t.RenderSize.Width > 0 && !string.IsNullOrEmpty(t.Text)))
                    {
                        var bounds = text.TransformToAncestor(host).TransformBounds(new Rect(text.RenderSize));
                        Check(bounds.Left >= -1 && bounds.Top >= -1 && bounds.Right <= width + 1 && bounds.Bottom <= 821,
                            "Text extends beyond the viewport: " + text.Text);
                    }
                    var path = Path.Combine(output, $"{mode}-{width}-{state}.png");
                    var bitmap = new RenderTargetBitmap(width, 820, 96, 96, PixelFormats.Pbgra32); bitmap.Render(host);
                    var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(bitmap));
                    using (var stream = File.Create(path)) png.Save(stream);
                    captures.Add(new { mode, width, height = 820, state, path = Path.GetFileName(path), dpi = 96 });
                }
                editor.SetValue((DependencyPropertyKey)typeof(UIElement).GetField("IsKeyboardFocusWithinPropertyKey",
                    BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!, false);
            }
            source.RootVisual = null;
        }
        File.WriteAllText(Path.Combine(output, "report.json"), JsonSerializer.Serialize(new {
            passed = true, checks, captures, runtime = Environment.Version.ToString(),
            focus = "Injected WPF template state on hidden HWND; not physical keyboard acceptance",
            validation = "Application-owned text and accessible field description; no framework validation API",
            releaseGate = false
        }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Passed {checks} appearance fixture checks; saved {captures.Count} native captures.");
    }

    private static void Layout(ViewHost host, int width)
    { host.Measure(new Size(width, 820)); host.Arrange(new Rect(0, 0, width, 820)); host.UpdateLayout(); }

    private static IEnumerable<T> Find<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T item) yield return item;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            foreach (var child in Find<T>(VisualTreeHelper.GetChild(root, i))) yield return child;
    }
}
