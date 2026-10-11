using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

[TestClass]
public sealed class NativeStyleTests
{
    [TestMethod]
    public void ScopeChangesRestoreNativeContentWithoutRecreatingTheIsland() => StaTestRunner.Run(() =>
    {
        var scope = new State<NativeStyleScope>(NativeStyleScope.Inherit);
        var creates = 0;
        var releases = 0;
        var clicks = 0;
        var input = new TextBox { Text = "Search query", FontSize = 16 };
        var button = new Button { Content = "Next result" };
        button.Click += (_, _) => clicks++;
        var native = new StackPanel();
        native.Children.Add(input);
        native.Children.Add(button);
        using var host = new ViewHost(() => VStack(
            WpfUI.Native(() => { creates++; return native; }, scope.Value, release: _ => releases++).Id("search"),
            Button("Framework action", () => { })));
        ThemeStyles.Apply(host, new ThemeTokens());
        host.Resources[typeof(Button)] = InvisibleStyle(typeof(Button));
        host.Resources[typeof(TextBox)] = InvisibleStyle(typeof(TextBox));
        using var source = new HwndSource(new HwndSourceParameters("Native style boundary")
        { Width = 500, Height = 300, PositionX = -10000, PositionY = -10000 });
        source.RootVisual = host;
        TestVisualTree.Layout(host);
        Assert.IsFalse(TestVisualTree.Find<TextBlock>(button).Any(text => text.Text == "Next result"));
        Assert.IsNull(input.Template.FindName("PART_ContentHost", input));

        input.Select(2, 3);
        scope.Value = NativeStyleScope.PreserveNative;
        TestVisualTree.Layout(host);
        Assert.IsTrue(TestVisualTree.Find<TextBlock>(button).Any(text => text.Text == "Next result"));
        Assert.IsNotNull(input.Template.FindName("PART_ContentHost", input));
        Assert.IsTrue(input.GetRectFromCharacterIndex(0).Height > 0);
        Assert.AreEqual(2, input.SelectionStart);
        Assert.AreEqual(3, input.SelectionLength);
        Assert.AreEqual("Search query", input.Text);
        Assert.AreEqual("Next result", new ButtonAutomationPeer(button).GetName());
        button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
        Assert.AreEqual(1, clicks);
        Assert.AreEqual(1, creates);
        Assert.AreEqual(0, releases);
        var sibling = TestVisualTree.Find<Button>(host).Single(value => !ReferenceEquals(value, button));
        Assert.AreSame(host.Resources[typeof(Button)], sibling.Style);

        ThemeStyles.Apply(host, new ThemeTokens { Ink = "#FFFFFF", Surface = "#181818" });
        TestVisualTree.Layout(host);
        Assert.IsTrue(TestVisualTree.Find<TextBlock>(button).Any(text => text.Text == "Next result"));
        scope.Value = NativeStyleScope.Inherit;
        TestVisualTree.Layout(host);
        Assert.AreSame(host.Resources[typeof(Button)], button.Style);
        Assert.AreEqual(1, creates);
        source.RootVisual = null;
        host.Dispose();
        Assert.AreEqual(1, releases);
        Assert.IsNull(native.Parent);
    });

    [TestMethod]
    public void ExplicitAndIslandLocalStylesWinInsidePreservedNativeScope() => StaTestRunner.Run(() =>
    {
        var local = new Style(typeof(Button));
        local.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Green));
        var explicitStyle = new Style(typeof(TextBox));
        explicitStyle.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.Blue));
        var button = new Button { Content = "Local" };
        var input = new TextBox { Text = "Explicit", Style = explicitStyle };
        var panel = new StackPanel();
        panel.Resources[typeof(Button)] = local;
        panel.Children.Add(button);
        panel.Children.Add(input);
        using var host = new ViewHost(() => WpfUI.Native(() => panel, NativeStyleScope.PreserveNative));
        ThemeStyles.Apply(host, new ThemeTokens());
        TestVisualTree.Layout(host);
        Assert.AreSame(local, button.Style);
        Assert.AreSame(explicitStyle, input.Style);
        Assert.AreSame(Brushes.Green, button.Background);
        Assert.AreSame(Brushes.Blue, input.Foreground);
    });

    [TestMethod]
    public void OriginalOverloadKeepsInheritedStylesAndInvalidScopeRunsNoFactory() => StaTestRunner.Run(() =>
    {
        var button = new Button { Content = "Inherited" };
        using var host = new ViewHost(() => WpfUI.Native(() => button));
        ThemeStyles.Apply(host, new ThemeTokens());
        TestVisualTree.Layout(host);
        Assert.AreSame(host.Resources[typeof(Button)], button.Style);
        var created = false;
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => WpfUI.Native(
            () => { created = true; return new Button(); }, (NativeStyleScope)123));
        Assert.IsFalse(created);
    });

    private static Style InvisibleStyle(Type type)
    {
        var template = new ControlTemplate(type) { VisualTree = new FrameworkElementFactory(typeof(Border)) };
        var style = new Style(type);
        style.Setters.Add(new Setter(Control.TemplateProperty, template));
        return style;
    }
}
