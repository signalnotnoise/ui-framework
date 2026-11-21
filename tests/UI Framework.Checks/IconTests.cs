using System.Windows;
using System.Runtime.CompilerServices;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

namespace UI_Framework.Checks;

[TestClass]
public sealed class IconTests
{
    private static readonly VectorIcon square = new("M 4,4 H 20 V 20 H 4 Z");
    private static readonly VectorIcon plus = new("M 12,4 V 20 M 4,12 H 20", strokeWidth: 2);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference[] CacheTemporaryDefinition()
    {
        var definition = new VectorIcon("M 2,2 H 22 V 22 Z");
        var geometry = IconGeometry.Get(definition);
        return [new WeakReference(definition), new WeakReference(geometry)];
    }

    [TestMethod]
    public void CacheDoesNotRetainDiscardedDefinitionsOrGeometry() => StaTestRunner.Run(() =>
    {
        var references = CacheTemporaryDefinition();
        for (var attempt = 0; attempt < 5 && references.Any(reference => reference.IsAlive); attempt++)
        {
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        }
        Assert.IsTrue(references.All(reference => !reference.IsAlive));
    });

    [TestMethod]
    public void FactoriesRejectInvalidDefinitionsSizesAndMissingNames()
    {
        Assert.ThrowsException<ArgumentException>(() => new VectorIcon(" "));
        foreach (var bad in new[] { 0, -1, double.NaN, double.PositiveInfinity })
        {
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new VectorIcon("M 0,0", viewportWidth: bad));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new VectorIcon("M 0,0", viewportHeight: bad));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => Icon(square, bad));
        }
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new VectorIcon("M 0,0", strokeWidth: -1));
        Assert.ThrowsException<ArgumentNullException>(() => Icon(null!));
        Assert.ThrowsException<ArgumentException>(() => IconButton(square, " ", () => { }));
        Assert.ThrowsException<ArgumentException>(() => Button(square, "", () => { }));
    }

    [TestMethod]
    public void InvalidIconDescriptionPreservesTheMountedTree() => StaTestRunner.Run(() =>
    {
        var view = Icon(square);
        using var host = new ViewHost(() => view);
        var frame = host.Content;
        view = Icon(new VectorIcon("not a path"));
        Assert.ThrowsException<FormatException>(host.Refresh);
        Assert.AreSame(frame, host.Content);
        view = IconButton(square, "Add", () => { }).AccessibilityLabel(null);
        Assert.ThrowsException<InvalidOperationException>(host.Refresh);
        view = Text("invalid") with { Icon = square };
        Assert.ThrowsException<InvalidOperationException>(host.Refresh);
        view = new View(ViewKind.Icon);
        Assert.ThrowsException<InvalidOperationException>(host.Refresh);
        Assert.AreSame(frame, host.Content);
        view = Icon(plus);
        host.Refresh();
        Assert.AreSame(frame, host.Content);
    });

    [TestMethod]
    public void GeometryIsFrozenSharedAndRetainedAcrossUpdates() => StaTestRunner.Run(() =>
    {
        var size = 16d;
        var definition = square;
        using var host = new ViewHost(() => HStack(Icon(definition, size), Icon(definition, size)));
        TestVisualTree.Layout(host);
        var icons = TestVisualTree.Find<IconElement>(host).ToArray();
        Assert.AreSame(icons[0].Geometry, icons[1].Geometry);
        Assert.IsTrue(icons[0].Geometry!.IsFrozen);
        var geometry = icons[0].Geometry;
        size = 32;
        host.Refresh(); TestVisualTree.Layout(host);
        Assert.AreSame(icons[0], TestVisualTree.Find<IconElement>(host).First());
        Assert.AreSame(geometry, icons[0].Geometry);
        Assert.AreEqual(32d, icons[0].DesiredSize.Width);
        definition = plus;
        host.Refresh();
        Assert.AreNotSame(geometry, icons[0].Geometry);
        Assert.AreSame(icons[0].Geometry, icons[1].Geometry);
    });

    [TestMethod]
    public void ButtonRetainsIdentityAcrossTextIconAndCombinedContent() => StaTestRunner.Run(() =>
    {
        var clicks = 0;
        var view = Button("Plain", () => clicks++);
        using var host = new ViewHost(() => view);
        var button = (Button)((Border)host.Content).Child;
        view = IconButton(plus, "Add", () => clicks += 10);
        host.Refresh(); TestVisualTree.Layout(host);
        var content = button.Content;
        Assert.AreEqual("Add", new ButtonAutomationPeer(button).GetName());
        Assert.AreEqual("Add", button.ToolTip);
        button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
        Assert.AreEqual(10, clicks);
        view = Button(square, "Save", () => clicks += 100);
        host.Refresh(); TestVisualTree.Layout(host);
        Assert.AreSame(content, button.Content);
        Assert.AreEqual("Save", new ButtonAutomationPeer(button).GetName());
        Assert.IsNull(button.ToolTip);
        view = Button("Back", () => clicks++);
        host.Refresh();
        Assert.AreSame(button, ((Border)host.Content).Child);
        Assert.AreEqual("Back", button.Content);
        Assert.AreEqual(DependencyProperty.UnsetValue, button.ReadLocalValue(AutomationProperties.NameProperty));
        host.Dispose();
        button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
        Assert.AreEqual(10, clicks, "Detached buttons must not call application callbacks.");
    });

    [TestMethod]
    public void StandaloneIconsAreDecorativeUnlessNamed() => StaTestRunner.Run(() =>
    {
        var view = Icon(square);
        using var host = new ViewHost(() => view);
        var icon = (IconElement)((Border)host.Content).Child;
        var peer = UIElementAutomationPeer.CreatePeerForElement(icon)!;
        Assert.IsFalse(peer.IsControlElement());
        Assert.IsFalse(icon.Focusable);
        view = view.AccessibilityLabel("Connected");
        host.Refresh();
        Assert.AreEqual("Connected", peer.GetName());
        Assert.AreEqual(AutomationControlType.Image, peer.GetAutomationControlType());
        Assert.IsTrue(peer.IsContentElement());
        view = view.AccessibilityLabel(null);
        host.Refresh();
        Assert.IsFalse(peer.IsControlElement());
    });

    [TestMethod]
    public void NativeButtonKeepsFocusInvokeAndDisabledSemantics() => StaTestRunner.Run(() =>
    {
        var clicks = 0;
        using var host = new ViewHost(() => IconButton(plus, "Add", () => clicks++));
        ThemeStyles.Apply(host, new ThemeTokens());
        var window = new Window { Content = host, Width = 240, Height = 140, ShowInTaskbar = false };
        try
        {
            window.Show(); TestVisualTree.Layout(host);
            var button = TestVisualTree.Find<Button>(host).Single();
            var peer = new ButtonAutomationPeer(button);
            Assert.IsTrue(button.Focusable);
            Assert.IsTrue(button.IsTabStop);
            Assert.IsTrue(button.Focus());
            Assert.AreEqual(AutomationControlType.Button, peer.GetAutomationControlType());
            ((IInvokeProvider)peer.GetPattern(PatternInterface.Invoke)!).Invoke();
            TestVisualTree.Flush();
            Assert.AreEqual(1, clicks);
            Assert.IsNull(UIElementAutomationPeer.CreatePeerForElement(TestVisualTree.Find<IconElement>(button).Single()));
            button.IsEnabled = false;
            Assert.ThrowsException<ElementNotEnabledException>(() => ((IInvokeProvider)peer.GetPattern(PatternInterface.Invoke)!).Invoke());
        }
        finally { window.Close(); }
    });

    [TestMethod]
    public void IconColorTracksButtonThemeAppearanceAndContrast() => StaTestRunner.Run(() =>
    {
        var view = Button(plus, "Add", () => { });
        using var host = new ViewHost(() => view);
        ThemeStyles.Apply(host, new ThemeTokens());
        TestVisualTree.Layout(host);
        var button = TestVisualTree.Find<Button>(host).Single();
        var icon = TestVisualTree.Find<IconElement>(button).Single();
        Assert.AreSame(button.Foreground, TextElement.GetForeground(icon));
        view = view.ButtonStyle(ButtonStyleKind.Primary);
        host.Refresh(); TestVisualTree.Layout(host);
        Assert.AreSame(button.Foreground, TextElement.GetForeground(icon));
        ThemeStyles.Apply(host, new ThemeTokens { OnAccent = "#12AB34" });
        TestVisualTree.Layout(host);
        Assert.AreEqual(Color.FromRgb(18, 171, 52), ((SolidColorBrush)TextElement.GetForeground(icon)).Color);
        var palette = (ThemePalette)host.Resources[ThemePalette.ResourceKey];
        palette.Refresh(true); TestVisualTree.Flush();
        Assert.AreSame(button.Foreground, TextElement.GetForeground(icon));
        view = view.IsEnabled(false);
        host.Refresh(); TestVisualTree.Layout(host);
        Assert.AreEqual(SystemColors.GrayTextColor, ((SolidColorBrush)TextElement.GetForeground(icon)).Color);
        palette.Refresh(false);
    });

    [TestMethod]
    public void FilledAndOutlinedIconsRenderAtMultipleDpiWithInheritedColor() => StaTestRunner.Run(() =>
    {
        foreach (var definition in new[] { square, plus })
        foreach (var dpi in new[] { 96d, 144d, 192d })
        {
            using var host = new ViewHost(() => Icon(definition, 24).Foreground("#FF0000"));
            host.Measure(new Size(24, 24)); host.Arrange(new Rect(0, 0, 24, 24)); host.UpdateLayout();
            var pixels = (int)(24 * dpi / 96);
            var bitmap = new RenderTargetBitmap(pixels, pixels, dpi, dpi, PixelFormats.Pbgra32);
            bitmap.Render(host);
            var data = new byte[pixels * pixels * 4];
            bitmap.CopyPixels(data, pixels * 4, 0);
            var center = ((pixels / 2) * pixels + pixels / 2) * 4;
            Assert.IsTrue(data[center + 2] > 200 && data[center + 3] > 200);
            Assert.AreEqual((byte)0, data[3], "Viewport padding must stay transparent.");
        }
    });

    [TestMethod]
    public void CombinedLabelWrapsWithinFiniteWidthWithoutShrinkingIcon() => StaTestRunner.Run(() =>
    {
        using var host = new ViewHost(() => Button(plus, "A long action label that must wrap within a compact toolbar", () => { }).Width(160));
        ThemeStyles.Apply(host, new ThemeTokens());
        TestVisualTree.Layout(host);
        var label = TestVisualTree.Find<TextBlock>(host).Single();
        var icon = TestVisualTree.Find<IconElement>(host).Single();
        Assert.IsTrue(label.ActualHeight > label.FontSize * 2);
        Assert.IsTrue(label.ActualWidth < 160);
        Assert.AreEqual(16d, icon.ActualWidth);
    });
}
