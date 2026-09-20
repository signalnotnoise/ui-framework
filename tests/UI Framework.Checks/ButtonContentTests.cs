using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using UI_Framework;
using UI_Framework.Wpf;

[TestClass]
public sealed class ButtonContentTests
{
    [TestMethod]
    public void RichNativeContentRemainsVisibleAndInteractiveAcrossThemeUpdates() => StaTestRunner.Run(() =>
    {
        var closes = 0;
        var filename = new TextBlock { Text = "Program.cs" };
        var close = new Button { Content = "Close" };
        close.Click += (_, _) => closes++;
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        row.Children.Add(filename);
        row.Children.Add(close);
        var tab = new Button { Content = row };
        var root = new ContentControl { Content = tab };
        ThemeStyles.Apply(root, new ThemeTokens());
        TestVisualTree.Layout(root);
        Assert.AreSame(row, tab.Content);
        Assert.IsTrue(TestVisualTree.Find<TextBlock>(root).Contains(filename));
        Assert.IsTrue(filename.ActualWidth > 0);
        Assert.IsTrue(close.ActualWidth > 0);
        close.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.AreEqual(1, closes);
        ThemeStyles.Apply(root, new ThemeTokens { Ink = "#EDF1F7", Surface = "#20252E" });
        TestVisualTree.Layout(root);
        Assert.AreSame(row, tab.Content);
        Assert.IsTrue(filename.ActualWidth > 0);
        tab.Content = "Renamed file";
        TestVisualTree.Layout(root);
        Assert.AreEqual("Renamed file", new ButtonAutomationPeer(tab).GetName());
        Assert.IsTrue(TestVisualTree.Find<TextBlock>(root).Any(text => text.Text == "Renamed file"));
        tab.Content = row;
        TestVisualTree.Layout(root);
        Assert.IsTrue(TestVisualTree.Find<TextBlock>(root).Contains(filename));
    });

    [TestMethod]
    public void GeneratedStringContentWrapsAndUsesThemedForeground() => StaTestRunner.Run(() =>
    {
        var button = new Button { Content = "A long button label should remain readable within a narrow width", Width = 140 };
        var root = new ContentControl { Content = button };
        var theme = new ThemeTokens { Ink = "#EDF1F7" };
        ThemeStyles.Apply(root, theme);
        TestVisualTree.Layout(root);
        var text = TestVisualTree.Find<TextBlock>(button).Single();
        Assert.AreEqual(TextWrapping.Wrap, text.TextWrapping);
        Assert.IsTrue(text.ActualHeight > text.FontSize * 2);
        Assert.AreEqual((Color)ColorConverter.ConvertFromString(theme.Ink), ((SolidColorBrush)text.Foreground).Color);
        ThemeStyles.SetAppearance(button, ButtonStyleKind.Primary);
        TestVisualTree.Layout(root);
        Assert.AreEqual((Color)ColorConverter.ConvertFromString(theme.OnAccent), ((SolidColorBrush)text.Foreground).Color);
    });

    [TestMethod]
    public void ApplicationTemplatesAndNativeTextKeepTheirWrappingPolicy() => StaTestRunner.Run(() =>
    {
        var textFactory = new FrameworkElementFactory(typeof(TextBlock));
        textFactory.SetValue(TextBlock.TextWrappingProperty, TextWrapping.NoWrap);
        textFactory.SetBinding(TextBlock.TextProperty, new Binding { StringFormat = "App: {0}" });
        var template = new DataTemplate { VisualTree = textFactory };
        var button = new Button { Content = "label", ContentTemplateSelector = new ButtonTestTemplateSelector(template) };
        var root = new ContentControl { Content = button };
        ThemeStyles.Apply(root, new ThemeTokens());
        TestVisualTree.Layout(root);
        Assert.AreEqual("App: label", TestVisualTree.Find<TextBlock>(button).Single().Text);
        Assert.AreEqual(TextWrapping.NoWrap, TestVisualTree.Find<TextBlock>(button).Single().TextWrapping);
        root.Resources.Add(new DataTemplateKey(typeof(string)), template);
        button.ContentTemplateSelector = null;
        TestVisualTree.Layout(root);
        Assert.AreEqual("App: label", TestVisualTree.Find<TextBlock>(button).Single().Text);
        Assert.AreEqual(TextWrapping.NoWrap, TestVisualTree.Find<TextBlock>(button).Single().TextWrapping);
        var native = new TextBlock { Text = "native", TextWrapping = TextWrapping.NoWrap };
        button.Content = native;
        TestVisualTree.Layout(root);
        Assert.AreSame(native, TestVisualTree.Find<TextBlock>(button).Single());
        Assert.AreEqual(TextWrapping.NoWrap, native.TextWrapping);
        button.Content = null;
        TestVisualTree.Layout(root);
        Assert.IsFalse(TestVisualTree.Find<TextBlock>(button).Any());
    });

    [TestMethod]
    public void ContentTemplateAndStringFormatAreRespected() => StaTestRunner.Run(() =>
    {
        var button = new Button { Content = 12, ContentStringFormat = "Score: {0}" };
        var root = new ContentControl { Content = button };
        ThemeStyles.Apply(root, new ThemeTokens());
        TestVisualTree.Layout(root);
        Assert.IsTrue(TestVisualTree.Find<TextBlock>(button).Any(text => text.Text == "Score: 12"));
        var textFactory = new FrameworkElementFactory(typeof(TextBlock));
        textFactory.SetBinding(TextBlock.TextProperty, new Binding { StringFormat = "Custom: {0}" });
        button.ContentTemplate = new DataTemplate { VisualTree = textFactory };
        TestVisualTree.Layout(root);
        Assert.IsTrue(TestVisualTree.Find<TextBlock>(button).Any(text => text.Text == "Custom: 12"));
        button.Content = 17;
        TestVisualTree.Layout(root);
        Assert.IsTrue(TestVisualTree.Find<TextBlock>(button).Any(text => text.Text == "Custom: 17"));
        button.ContentTemplate = null;
        TestVisualTree.Layout(root);
        Assert.IsTrue(TestVisualTree.Find<TextBlock>(button).Any(text => text.Text == "Score: 17"));
    });
}
