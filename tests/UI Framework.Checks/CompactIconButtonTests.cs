using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

[TestClass]
public sealed class CompactIconButtonTests
{
    private const double Tolerance = 0.01;
    private const double CaptureHeight = 64;
    private static readonly VectorIcon square = new("M0,0 L24,0 L24,24 L0,24 Z");
    public TestContext TestContext { get; set; } = null!;

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void OuterWidthDoesNotSetNativeWidthOrOverrideLocalPadding(bool customTemplate) => StaTestRunner.Run(() =>
    {
        var size = SkillIconToolbarExample.TargetSize;
        using var host = new ViewHost(() => IconButton(square, "Action", () => { })
            .FontSize(IconTokens.Default.Small).Width(size));
        ThemeStyles.Apply(host, new ThemeTokens());
        var style = new Style(typeof(Button), (Style)host.Resources[typeof(Button)]);
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        if (customTemplate)
        {
            // Reconstructed consumer sizing conditions, not GuidedGrade's actual template.
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));
            var content = new FrameworkElementFactory(typeof(ContentPresenter));
            content.SetValue(ContentPresenter.ContentProperty, new TemplateBindingExtension(ContentControl.ContentProperty));
            content.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            content.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(content);
            style.Setters.Add(new Setter(Control.TemplateProperty, new ControlTemplate(typeof(Button)) { VisualTree = border }));
        }
        host.Resources[typeof(Button)] = style;
        TestVisualTree.Layout(host);
        var frame = (Border)host.Content;
        var button = (Button)frame.Child;
        Assert.AreEqual(size, frame.ActualWidth, Tolerance);
        Assert.IsTrue(double.IsNaN(button.Width));
        Assert.AreEqual(BaseValueSource.Local, DependencyPropertyHelper.GetValueSource(button, Control.PaddingProperty).BaseValueSource);
        Assert.IsTrue(button.Padding.Left + button.Padding.Right > 0);
        Assert.AreEqual(HorizontalAlignment.Left, button.HorizontalAlignment);
        TestContext.WriteLine($"customTemplate={customTemplate}: frame={frame.ActualWidth}, native={button.ActualWidth}, padding={button.Padding}");

        button.ClearValue(Control.PaddingProperty);
        TestVisualTree.Layout(host);
        Assert.AreEqual(new Thickness(0), button.Padding);
        Assert.IsTrue(button.ActualWidth < frame.ActualWidth,
            "The left-aligned native button remains content-sized, not frame-sized.");
        TestContext.WriteLine($"After clearing padding: frame={frame.ActualWidth}, native={button.ActualWidth}");
        button.Width = size;
        TestVisualTree.Layout(host);
        Assert.AreEqual(size, button.ActualWidth, Tolerance);
    });

    [DataTestMethod]
    [DataRow(320d, 1d, false)]
    [DataRow(800d, 1d, false)]
    [DataRow(320d, 2d, false)]
    [DataRow(800d, 2d, false)]
    [DataRow(320d, 1d, true)]
    [DataRow(800d, 1d, true)]
    [DataRow(320d, 2d, true)]
    [DataRow(800d, 2d, true)]
    public void ScopedRecipePreservesHitTargetIconAndNativeFeedback(double width, double scale, bool dark) => StaTestRunner.Run(() =>
    {
        var theme = dark ? new ThemeTokens { Canvas = "#161A20", Surface = "#20252E", Ink = "#EDF1F7" } : new ThemeTokens();
        var clicks = 0;
        var example = new SkillIconToolbarExample(theme, () => clicks++);
        using var host = new ViewHost(example.Body);
        example.ApplyTheme(host, theme, false);
        using var source = new HwndSource(new HwndSourceParameters("Compact icon toolbar acceptance")
        {
            Width = (int)width, Height = (int)CaptureHeight,
            PositionX = -10000, PositionY = -10000, WindowStyle = unchecked((int)0x80000000)
        });
        source.RootVisual = host;
        TestVisualTree.Flush();
        Layout(host, width, scale);
        var buttons = TestVisualTree.Find<Button>(host).ToArray();
        var button = buttons[0];
        var icon = TestVisualTree.Find<IconElement>(button).Single();
        Assert.AreEqual(DependencyProperty.UnsetValue, button.ReadLocalValue(Control.PaddingProperty));
        Assert.AreEqual(new Thickness(0), button.Padding);
        AssertGeometry(button, icon, theme.Icons.Small);
        Assert.IsTrue(button.Focusable && button.IsTabStop);
        Assert.IsFalse(buttons[1].IsEnabled);
        var peer = new ButtonAutomationPeer(button);
        Assert.AreEqual("Search", peer.GetName());
        ((IInvokeProvider)peer.GetPattern(PatternInterface.Invoke)!).Invoke();
        TestVisualTree.Flush();
        Assert.AreEqual(1, clicks);

        var chrome = (Border)button.Template.FindName("Chrome", button);
        Assert.AreEqual(Colors.Transparent, ((SolidColorBrush)chrome.BorderBrush).Color);
        Assert.IsTrue(chrome.BorderThickness.Left > 0, "Transparent border reserves the focus outline.");
        Capture(host, width, scale, dark, "quiet");
        SetState(button, "IsMouseOver", true);
        Assert.AreEqual(Parse(theme.Hover), ((SolidColorBrush)chrome.Background).Color);
        SetState(button, "IsMouseOver", false);
        SetState(button, "IsKeyboardFocused", true);
        Assert.AreEqual(Parse(theme.Focus), ((SolidColorBrush)chrome.BorderBrush).Color);
        Capture(host, width, scale, dark, "focus");
        SetState(button, "IsKeyboardFocused", false);

        // Reapplying the recipe rebases on the new framework style, not a sealed old palette.
        var changed = theme with { Icons = theme.Icons with { Small = theme.Icons.Medium } };
        example.ApplyTheme(host, changed, false);
        Layout(host, width, scale);
        Assert.AreSame(button, TestVisualTree.Find<Button>(host).First());
        Assert.AreSame(icon, TestVisualTree.Find<IconElement>(button).Single());
        AssertGeometry(button, icon, changed.Icons.Small);
        example.ApplyTheme(host, changed, true);
        Layout(host, width, scale);
        var palette = (ThemePalette)host.Resources[ThemePalette.ResourceKey];
        palette.Refresh(true);
        TestVisualTree.Flush();
        Assert.AreEqual(SystemColors.WindowTextColor, ((SolidColorBrush)button.Foreground).Color);
        Assert.AreEqual(SystemColors.WindowColor, ((SolidColorBrush)((Border)host.Content).Background).Color);
        Assert.AreEqual(SystemColors.GrayTextColor, ((SolidColorBrush)buttons[1].Foreground).Color);
        SetState(button, "IsKeyboardFocused", true);
        chrome = (Border)button.Template.FindName("Chrome", button);
        Assert.AreEqual(SystemColors.HighlightColor, ((SolidColorBrush)chrome.BorderBrush).Color);
        Capture(host, width, scale, dark, "contrast-focus");
        SetState(button, "IsKeyboardFocused", false);
        palette.Refresh(false);
        host.Dispose();
        button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
        Assert.AreEqual(1, clicks, "Disposed toolbar callbacks must be detached.");
    });

    private static void AssertGeometry(Button button, IconElement icon, double iconSize)
    {
        Assert.AreEqual(SkillIconToolbarExample.TargetSize, button.ActualWidth, Tolerance);
        Assert.AreEqual(SkillIconToolbarExample.TargetSize, button.ActualHeight, Tolerance);
        Assert.AreEqual(iconSize, icon.ActualWidth, Tolerance);
        Assert.AreEqual(iconSize, icon.ActualHeight, Tolerance);
        var bounds = icon.TransformToAncestor(button).TransformBounds(new Rect(icon.RenderSize));
        Assert.IsTrue(bounds.Left >= 0 && bounds.Top >= 0 && bounds.Right <= button.ActualWidth && bounds.Bottom <= button.ActualHeight,
            $"Icon bounds {bounds} exceed native hit target {button.RenderSize}.");
    }

    private static void Layout(ViewHost host, double width, double scale)
    {
        host.Measure(new Size(width, CaptureHeight));
        host.Arrange(new Rect(0, 0, width, CaptureHeight));
        host.UpdateLayout();
        VisualTreeHelper.SetRootDpi(host, new DpiScale(scale, scale));
        host.UpdateLayout();
    }

    private static Color Parse(string value) => (Color)ColorConverter.ConvertFromString(value);

    private static void SetState(Button button, string property, bool value)
    {
        // Tests template triggers on a hidden source; not physical focus/mouse acceptance.
        var key = (DependencyPropertyKey)typeof(UIElement).GetField(property + "PropertyKey", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        button.SetValue(key, value);
        button.UpdateLayout();
    }

    private static void Capture(ViewHost host, double width, double scale, bool dark, string state)
    {
        host.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)(width * scale), (int)(CaptureHeight * scale), 96 * scale, 96 * scale, PixelFormats.Pbgra32);
        bitmap.Render(host);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        var folder = System.IO.Path.Combine(AppContext.BaseDirectory, "compact-icon-render");
        System.IO.Directory.CreateDirectory(folder);
        using var file = System.IO.File.Create(System.IO.Path.Combine(folder, $"{(dark ? "dark" : "light")}-{width}-{scale}-{state}.png"));
        encoder.Save(file);
    }
}
