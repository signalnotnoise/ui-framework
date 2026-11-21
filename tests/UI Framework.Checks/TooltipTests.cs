using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Input;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

[TestClass]
public sealed class TooltipTests
{
    private static readonly VectorIcon search = new("M10,3 A7,7 0 1 0 10,17 A7,7 0 1 0 10,3 M15,15 L21,21", strokeWidth: 2);

    [TestMethod]
    public void HintUpdatesRetainIconAndAccessibleNameAndCanRestoreOrSuppressFallback() => StaTestRunner.Run(() =>
    {
        var view = IconButton(search, "Search", () => { }).Tooltip("Find in document (Ctrl+F)");
        using var host = new ViewHost(() => view);
        TestVisualTree.Layout(host);
        var button = TestVisualTree.Find<Button>(host).Single();
        var content = button.Content;
        Assert.AreEqual("Find in document (Ctrl+F)", button.ToolTip);
        Assert.AreEqual("Search", new ButtonAutomationPeer(button).GetName());
        view = view.Tooltip("Find in all documents").AccessibilityLabel("Global search");
        host.Refresh(); TestVisualTree.Layout(host);
        Assert.AreSame(button, TestVisualTree.Find<Button>(host).Single());
        Assert.AreSame(content, button.Content);
        Assert.AreEqual("Find in all documents", button.ToolTip);
        Assert.AreEqual("Global search", new ButtonAutomationPeer(button).GetName());
        view = view.Tooltip(null);
        host.Refresh();
        Assert.AreEqual("Global search", button.ToolTip);
        view = view.Tooltip("");
        host.Refresh();
        Assert.IsNull(button.ToolTip);
        Assert.IsNull(button.ReadLocalValue(FrameworkElement.ToolTipProperty));
        Assert.AreEqual("Global search", new ButtonAutomationPeer(button).GetName());
    });

    [TestMethod]
    public void ExplicitHintsOwnLocalValuesEvenWhenInitiallyEqualToStyleAndNullRestoresStyle() => StaTestRunner.Run(() =>
    {
        var view = Button("Save", () => { });
        using var host = new ViewHost(() => view);
        TestVisualTree.Layout(host);
        var button = TestVisualTree.Find<Button>(host).Single();
        static Style HintStyle(string hint)
        {
            var style = new Style(typeof(Button));
            style.Setters.Add(new Setter(FrameworkElement.ToolTipProperty, hint));
            return style;
        }
        button.Style = HintStyle("Save file");
        view = view.Tooltip("Save file");
        host.Refresh();
        Assert.AreEqual("Save file", button.ReadLocalValue(FrameworkElement.ToolTipProperty));
        button.Style = HintStyle("Changed style hint");
        Assert.AreEqual("Save file", button.ToolTip);
        view = view.Tooltip(null);
        host.Refresh();
        Assert.AreEqual("Changed style hint", button.ToolTip);
        view = view.Tooltip("");
        host.Refresh();
        button.Style = HintStyle("Another hint");
        Assert.IsNull(button.ToolTip);
    });

    [TestMethod]
    public void PlainIconAndCombinedTransitionsDoNotLeaveStaleHints() => StaTestRunner.Run(() =>
    {
        var view = Button("Plain", () => { }).Tooltip("Plain hint");
        using var host = new ViewHost(() => view);
        TestVisualTree.Layout(host);
        var button = TestVisualTree.Find<Button>(host).Single();
        view = IconButton(search, "Search", () => { });
        host.Refresh();
        Assert.AreEqual("Search", button.ToolTip);
        view = Button(search, "Search all", () => { }).Tooltip("Search every file");
        host.Refresh();
        Assert.AreEqual("Search every file", button.ToolTip);
        view = view.Tooltip(null);
        host.Refresh();
        Assert.IsNull(button.ToolTip);
        view = Button("Plain again", () => { });
        host.Refresh();
        Assert.AreSame(button, TestVisualTree.Find<Button>(host).Single());
        Assert.AreEqual(DependencyProperty.UnsetValue, button.ReadLocalValue(FrameworkElement.ToolTipProperty));
    });

    [TestMethod]
    public void NativeKeyboardDisabledAndPresentationPoliciesRemainOwnedByWpfAndApplicationStyles() => StaTestRunner.Run(() =>
    {
        var view = Button("Save", () => { }).Tooltip("Save file (Ctrl+S)");
        using var host = new ViewHost(() => view);
        ThemeStyles.Apply(host, new ThemeTokens());
        TestVisualTree.Layout(host);
        var button = TestVisualTree.Find<Button>(host).Single();
        var native = new Button();
        Assert.IsTrue(button.Focusable && button.IsTabStop);
        Assert.AreEqual(ToolTipService.GetInitialShowDelay(native), ToolTipService.GetInitialShowDelay(button));
        Assert.AreEqual(ToolTipService.GetShowDuration(native), ToolTipService.GetShowDuration(button));
        Assert.AreEqual(ToolTipService.GetShowsToolTipOnKeyboardFocus(native), ToolTipService.GetShowsToolTipOnKeyboardFocus(button));
        Assert.AreEqual(ToolTipService.GetShowOnDisabled(native), ToolTipService.GetShowOnDisabled(button));
        ToolTipService.SetShowsToolTipOnKeyboardFocus(button, true);
        ToolTipService.SetShowOnDisabled(button, true);
        view = view.Tooltip("Saving unavailable").IsEnabled(false);
        host.Refresh(); TestVisualTree.Layout(host);
        ((ThemePalette)host.Resources[ThemePalette.ResourceKey]).Refresh(true);
        Assert.IsFalse(button.IsEnabled);
        Assert.AreEqual("Saving unavailable", button.ToolTip);
        Assert.AreEqual(true, ToolTipService.GetShowsToolTipOnKeyboardFocus(button));
        Assert.IsTrue(ToolTipService.GetShowOnDisabled(button));
        Assert.AreEqual("Save", new ButtonAutomationPeer(button).GetName());
        Assert.AreEqual(DependencyProperty.UnsetValue, button.ReadLocalValue(ToolTipService.ShowDurationProperty));
    });

    [TestMethod]
    public void NativeFocusTraversalAndHintUpdatesKeepTheSameButtonFocused() => StaTestRunner.Run(() =>
    {
        var hint = "Search current document";
        using var host = new ViewHost(() => HStack(
            Button("Save", () => { }).Tooltip("Save document"),
            IconButton(search, "Search", () => { }).Tooltip(hint)));
        var window = new Window
        {
            Content = host, Width = 400, Height = 200,
            Left = -10000, Top = -10000, ShowInTaskbar = false
        };
        try
        {
            window.Show();
            window.Activate();
            window.UpdateLayout();
            var buttons = TestVisualTree.Find<Button>(host).ToArray();
            Assert.IsTrue(buttons[0].Focus());
            Assert.IsTrue(buttons[0].MoveFocus(new TraversalRequest(FocusNavigationDirection.Next)));
            Assert.IsTrue(buttons[1].IsKeyboardFocused);
            hint = "Search all documents";
            host.Refresh(); window.UpdateLayout();
            Assert.AreSame(buttons[1], Keyboard.FocusedElement);
            Assert.AreEqual(hint, buttons[1].ToolTip);
            Assert.AreEqual("Search", new ButtonAutomationPeer(buttons[1]).GetName());
        }
        finally { window.Close(); }
    });

    [TestMethod]
    public void VirtualRowsUpdateRegenerateAndReuseKeysWithoutPreviousHintsOrCallbacks() => StaTestRunner.Run(() =>
    {
        var rows = new StateList<int>(Enumerable.Range(0, 100));
        var generation = 0;
        var clicks = 0;
        using var host = new ViewHost(() => VirtualList(rows.Select(id =>
            Button($"Row {id}", () => clicks++).Tooltip($"Generation {generation}: row {id}").Height(32).Id(id.ToString())), 160));
        using var source = new HwndSource(new HwndSourceParameters("Tooltip row acceptance")
        {
            Width = 500, Height = 300, PositionX = -10000, PositionY = -10000,
            WindowStyle = unchecked((int)0x80000000)
        });
        source.RootVisual = host;
        TestVisualTree.Layout(host);
        var original = TestVisualTree.Find<Button>(host).First();
        Assert.AreEqual("Generation 0: row 0", original.ToolTip);
        generation++;
        host.Refresh(); TestVisualTree.Layout(host);
        Assert.AreSame(original, TestVisualTree.Find<Button>(host).First());
        Assert.AreEqual("Generation 1: row 0", original.ToolTip);
        TestVisualTree.Find<ItemsControl>(host).Single().Items.Refresh();
        TestVisualTree.Layout(host);
        var regenerated = TestVisualTree.Find<Button>(host).First();
        Assert.AreNotSame(original, regenerated);
        Assert.AreEqual("Generation 1: row 0", regenerated.ToolTip);
        rows.Remove(0);
        host.Refresh(); TestVisualTree.Layout(host);
        original.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
        regenerated.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
        Assert.AreEqual(0, clicks);
        generation++;
        rows.ReplaceAll([0, .. rows]);
        host.Refresh(); TestVisualTree.Layout(host);
        var fresh = TestVisualTree.Find<Button>(host).First();
        Assert.AreNotSame(regenerated, fresh);
        Assert.AreEqual("Generation 2: row 0", fresh.ToolTip);
        host.Dispose();
        fresh.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
        Assert.AreEqual(0, clicks);
    });

    [TestMethod]
    public void UnsupportedDirectDescriptionFailsBeforeReplacingPreviousTreeAndNativeOwnershipIsUntouched() => StaTestRunner.Run(() =>
    {
        var native = new Button { Content = "Native", ToolTip = "Application-owned hint" };
        var view = WpfUI.Native(() => native);
        using var host = new ViewHost(() => view);
        var content = host.Content;
        view = Text("Invalid") with { TooltipText = "Not supported on text yet" };
        Assert.ThrowsException<InvalidOperationException>(host.Refresh);
        Assert.AreSame(content, host.Content);
        Assert.AreEqual("Application-owned hint", native.ToolTip);
        Assert.ThrowsException<InvalidOperationException>(() => Text("Text").Tooltip("Hint"));
    });
}
