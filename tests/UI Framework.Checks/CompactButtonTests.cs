using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UI_Framework;
using UI_Framework.Wpf;

[TestClass]
public sealed class CompactButtonTests
{
    [TestMethod]
    public void CompactNativeButtonHonorsPaddingAndExplicitHeight() => StaTestRunner.Run(() =>
    {
        var button = new Button { Content = "×", FontSize = 10, Width = 18, Height = 18, Padding = new Thickness(0) };
        var root = new ContentControl { Content = button };
        ThemeStyles.Apply(root, new ThemeTokens());
        TestVisualTree.Layout(root);
        var chrome = (Border)button.Template.FindName("Chrome", button);
        Assert.AreEqual(button.Padding, chrome.Padding, "The chrome must respect the native padding override.");
        Assert.AreEqual(18.0, button.ActualHeight, 0.01, "Theme minimum must not override an explicit compact height.");
        var text = TestVisualTree.Find<TextBlock>(button).Single();
        Assert.IsTrue(text.ActualWidth > 0 && text.ActualHeight > 0);
        var bounds = text.TransformToAncestor(button).TransformBounds(new Rect(text.RenderSize));
        Assert.IsTrue(bounds.Left >= 0 && bounds.Top >= 0 && bounds.Right <= button.ActualWidth && bounds.Bottom <= button.ActualHeight,
            $"Glyph bounds {bounds} exceed {button.ActualWidth}x{button.ActualHeight}.");
    });

    [TestMethod]
    public void DeclarativeButtonKeepsItsNativePaddingAcrossThemes() => StaTestRunner.Run(() =>
    {
        using var host = new ViewHost(() => UI_Framework.UI.Button("Action", () => { }));
        TestVisualTree.Layout(host);
        var button = TestVisualTree.Find<Button>(host).Single();
        Assert.AreEqual(new Thickness(12, 6, 12, 6), button.Padding);
        ThemeStyles.Apply(host, new ThemeTokens { ControlPadding = 10 });
        TestVisualTree.Layout(host);
        Assert.AreEqual(new Thickness(12, 6, 12, 6), button.Padding);
        Assert.AreEqual(button.Padding, ((Border)button.Template.FindName("Chrome", button)).Padding);
        var originalHeight = button.DesiredSize.Height;
        Assert.IsTrue(originalHeight >= 36);
        ThemeStyles.Apply(host, new ThemeTokens { ControlPadding = 14 });
        TestVisualTree.Layout(host);
        Assert.AreSame(button, TestVisualTree.Find<Button>(host).Single());
        Assert.AreEqual(new Thickness(12, 6, 12, 6), button.Padding);
        Assert.AreEqual(button.Padding, ((Border)button.Template.FindName("Chrome", button)).Padding);
        Assert.AreEqual(originalHeight, button.DesiredSize.Height, 0.01);
        button.ClearValue(Control.PaddingProperty);
        TestVisualTree.Layout(host);
        Assert.AreEqual(new Thickness(14), button.Padding);
        Assert.AreEqual(button.Padding, ((Border)button.Template.FindName("Chrome", button)).Padding);
        Assert.IsTrue(button.DesiredSize.Height > originalHeight);
    });

    [TestMethod]
    public void AutomaticSizingAndExplicitMinimumKeepTheirPrecedence() => StaTestRunner.Run(() =>
    {
        var button = new Button { Content = "×", FontSize = 10, Padding = new Thickness(0), VerticalAlignment = VerticalAlignment.Top };
        var root = new ContentControl { Content = button };
        ThemeStyles.Apply(root, new ThemeTokens());
        TestVisualTree.Layout(root);
        Assert.AreEqual(36.0, button.ActualHeight, 0.01);
        button.Height = 18;
        TestVisualTree.Layout(root);
        Assert.AreEqual(18.0, button.ActualHeight, 0.01);
        button.ClearValue(FrameworkElement.HeightProperty);
        TestVisualTree.Layout(root);
        Assert.AreEqual(36.0, button.ActualHeight, 0.01);
        button.Height = 18;
        button.MinHeight = 24;
        TestVisualTree.Layout(root);
        Assert.AreEqual(24.0, button.ActualHeight, 0.01);
        button.ClearValue(FrameworkElement.MinHeightProperty);
        TestVisualTree.Layout(root);
        Assert.AreEqual(18.0, button.ActualHeight, 0.01);
    });

    [TestMethod]
    public void PaddingBindingsRemainLiveAndNormalThemeSpacingIsUnchanged() => StaTestRunner.Run(() =>
    {
        var button = new Button { Content = "Action", FontSize = 16, VerticalAlignment = VerticalAlignment.Top };
        var root = new ContentControl { Content = button };
        ThemeStyles.Apply(root, new ThemeTokens());
        TestVisualTree.Layout(root);
        Assert.AreEqual(new Thickness(10), button.Padding);
        Assert.AreEqual(new Thickness(10), ((Border)button.Template.FindName("Chrome", button)).Padding);
        var normalSize = button.DesiredSize;
        ThemeStyles.Apply(root, new ThemeTokens { Ink = "#EDF1F7", Surface = "#20252E" });
        TestVisualTree.Layout(root);
        Assert.AreEqual(normalSize, button.DesiredSize);
        var source = new Border { Padding = new Thickness(3, 1, 5, 2) };
        var binding = button.SetBinding(Control.PaddingProperty, new Binding(nameof(Border.Padding)) { Source = source });
        TestVisualTree.Layout(root);
        Assert.AreEqual(source.Padding, ((Border)button.Template.FindName("Chrome", button)).Padding);
        source.Padding = new Thickness(0);
        TestVisualTree.Layout(root);
        Assert.AreSame(binding, button.GetBindingExpression(Control.PaddingProperty));
        Assert.AreEqual(source.Padding, ((Border)button.Template.FindName("Chrome", button)).Padding);
        ThemeStyles.Apply(root, new ThemeTokens { ControlPadding = 14 });
        TestVisualTree.Layout(root);
        Assert.AreEqual(new Thickness(0), button.Padding);
        BindingOperations.ClearBinding(button, Control.PaddingProperty);
        TestVisualTree.Layout(root);
        Assert.AreEqual(new Thickness(14), button.Padding);
        Assert.AreEqual(button.Padding, ((Border)button.Template.FindName("Chrome", button)).Padding);
    });

    [DataTestMethod]
    [DataRow(1.0, false)]
    [DataRow(1.25, false)]
    [DataRow(1.5, false)]
    [DataRow(2.0, false)]
    [DataRow(1.0, true)]
    [DataRow(1.25, true)]
    [DataRow(1.5, true)]
    [DataRow(2.0, true)]
    public void GlyphIsContainedAtScaledDpiInLightAndDarkThemes(double scale, bool dark) => StaTestRunner.Run(() =>
    {
        var theme = dark ? new ThemeTokens { Canvas = "#161A20", Surface = "#20252E", Ink = "#EDF1F7" } : new ThemeTokens();
        var button = new Button { Content = "×", FontSize = 10, Width = 18, Height = 18, Padding = new Thickness(0) };
        var root = new Canvas { Width = 40, Height = 32, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.Canvas)) };
        root.Children.Add(button);
        Canvas.SetLeft(button, 4);
        Canvas.SetTop(button, 4);
        VisualTreeHelper.SetRootDpi(root, new DpiScale(scale, scale));
        ThemeStyles.Apply(root, theme);
        root.Measure(new Size(40, 32));
        root.Arrange(new Rect(0, 0, 40, 32));
        root.UpdateLayout();
        // Materialize the template before propagating simulated root DPI to it.
        VisualTreeHelper.SetRootDpi(root, new DpiScale(scale, scale));
        root.UpdateLayout();
        var text = TestVisualTree.Find<TextBlock>(button).Single();
        var bounds = text.TransformToAncestor(button).TransformBounds(new Rect(text.RenderSize));
        Assert.AreEqual(scale, VisualTreeHelper.GetDpi(text).DpiScaleX, 0.001);
        Assert.AreEqual(18.0, button.ActualHeight, 0.01);
        Assert.IsTrue(text.ActualWidth > 0 && text.ActualHeight > 0);
        Assert.IsTrue(bounds.Left >= -0.01 && bounds.Top >= -0.01 && bounds.Right <= 18.01 && bounds.Bottom <= 18.01,
            $"Glyph bounds {bounds} at DPI scale {scale} exceed the compact button.");
        Assert.AreEqual((Color)ColorConverter.ConvertFromString(theme.Ink), ((SolidColorBrush)text.Foreground).Color);
        var bitmap = new RenderTargetBitmap((int)(40 * scale), (int)(32 * scale), 96 * scale, 96 * scale, PixelFormats.Pbgra32);
        bitmap.Render(root);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        var folder = System.IO.Path.Combine(AppContext.BaseDirectory, "compact-button-render");
        System.IO.Directory.CreateDirectory(folder);
        using var output = System.IO.File.Create(System.IO.Path.Combine(folder, $"{(dark ? "dark" : "light")}-{(int)(scale * 100)}.png"));
        encoder.Save(output);
        button.Padding = new Thickness(1);
        root.UpdateLayout();
        Assert.AreEqual(button.Padding, ((Border)button.Template.FindName("Chrome", button)).Padding);
    });

    [TestMethod]
    public void CompactRichContentRetainsKeyboardFocusAutomationAndThemeStates() => StaTestRunner.Run(() =>
    {
        var clicks = 0;
        var glyph = new TextBlock { Text = "×", FontSize = 10 };
        var button = new Button { Content = glyph, Width = 18, Height = 18, Padding = new Thickness(0) };
        AutomationProperties.SetName(button, "Close document");
        button.Click += (_, _) => clicks++;
        var theme = new ThemeTokens();
        var root = new ContentControl { Content = button };
        ThemeStyles.Apply(root, theme);
        var window = new Window { Content = root, Width = 140, Height = 100, Left = -10000, Top = -10000, ShowInTaskbar = false };
        try
        {
            window.Show();
            window.Activate();
            button.Focus();
            TestVisualTree.Layout(root);
            Assert.AreSame(button, Keyboard.FocusedElement);
            var chrome = (Border)button.Template.FindName("Chrome", button);
            Assert.AreEqual((Color)ColorConverter.ConvertFromString(theme.Focus), ((SolidColorBrush)chrome.BorderBrush).Color);
            var peer = new ButtonAutomationPeer(button);
            Assert.AreEqual("Close document", peer.GetName());
            ((IInvokeProvider)peer.GetPattern(PatternInterface.Invoke)).Invoke();
            TestVisualTree.Flush();
            Assert.AreEqual(1, clicks);
            ThemeStyles.Apply(root, theme with { Ink = "#EDF1F7", Surface = "#20252E" });
            TestVisualTree.Layout(root);
            Assert.AreSame(glyph, button.Content);
            Assert.AreEqual(18.0, button.ActualHeight, 0.01);
            Assert.AreSame(button, Keyboard.FocusedElement);
            button.IsEnabled = false;
            Assert.IsFalse(peer.IsEnabled());
            Assert.AreEqual(1.0, button.Opacity, 0.001);
        }
        finally { window.Close(); }
    });
}
