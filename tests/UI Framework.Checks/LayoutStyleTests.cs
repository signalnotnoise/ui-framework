using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

[TestClass]
public sealed class LayoutStyleTests
{
    private static void Layout(ViewHost host, double width)
    {
        host.Measure(new Size(width, 800));
        host.Arrange(new Rect(0, 0, width, 800));
        host.UpdateLayout();
        TestVisualTree.Flush();
    }

    [TestMethod]
    public void FlexRowsAllocateRemainingWidthAfterFixedChildrenAndGaps() => StaTestRunner.Run(() =>
    {
        using var host = new ViewHost(() => FlexRow(Text("fixed").Width(100), Text("one"), Text("two").Flex(2)).Spacing(10));
        Layout(host, 720);
        var text = TestVisualTree.Find<TextBlock>(host).ToArray();
        Assert.AreEqual(100, text[0].ActualWidth, 0.1);
        Assert.AreEqual(200, text[1].ActualWidth, 0.1);
        Assert.AreEqual(400, text[2].ActualWidth, 0.1);
    });

    [TestMethod]
    public void AdaptiveColumnsReflowWithoutReplacingTheInput() => StaTestRunner.Run(() =>
    {
        var state = new State<string>("selection survives resize");
        using var host = new ViewHost(() => AdaptiveGrid(200, TextField(state), Text("second"), Text("third")).Spacing(10));
        Layout(host, 660);
        var input = TestVisualTree.Find<TextBox>(host).Single();
        input.Select(2, 4);
        var second = TestVisualTree.Find<TextBlock>(host).Single(t => t.Text == "second");
        Assert.AreEqual(input.TranslatePoint(new Point(), host).Y, second.TranslatePoint(new Point(), host).Y, 0.1);
        Layout(host, 300);
        Assert.AreSame(input, TestVisualTree.Find<TextBox>(host).Single());
        Assert.IsTrue(second.TranslatePoint(new Point(), host).Y > input.TranslatePoint(new Point(), host).Y);
        Assert.AreEqual(4, input.SelectionLength);
        input.Text = "edited after resize";
        Assert.AreEqual("edited after resize", state.Value);
    });

    [TestMethod]
    public void ScopedThemeUpdatesRetainedControlsAndKeepsTextBinding() => StaTestRunner.Run(() =>
    {
        var primary = new State<bool>(true);
        var enabled = new State<bool>(true);
        var text = new State<string>("before");
        using var host = new ViewHost(() => VStack(UI_Framework.UI.Button("Action", () => { })
            .ButtonStyle(primary.Value ? ButtonStyleKind.Primary : ButtonStyleKind.Secondary).IsEnabled(enabled.Value), TextField(text)));
        ThemeStyles.Apply(host, new ThemeTokens { Accent = "#123456" });
        Layout(host, 400);
        var button = TestVisualTree.Find<Button>(host).Single();
        Assert.AreEqual(Color.FromRgb(18, 52, 86), ((SolidColorBrush)button.Background).Color);
        enabled.Value = false;
        primary.Value = false;
        TestVisualTree.Flush();
        Assert.AreSame(button, TestVisualTree.Find<Button>(host).Single());
        Assert.IsFalse(button.IsEnabled);
        Assert.AreEqual(0.45, button.Opacity, 0.001);
        var input = TestVisualTree.Find<TextBox>(host).Single();
        input.Text = "after";
        Assert.AreEqual("after", text.Value);
        using var other = new ViewHost(() => UI_Framework.UI.Button("Unstyled", () => { }));
        Assert.IsFalse(other.Resources.Contains(typeof(Button)));
    });

    [TestMethod]
    public void ExplicitForegroundOverridesThemeAndCanBeRemoved() => StaTestRunner.Run(() =>
    {
        var color = new State<string?>("#FF0000");
        var value = new State<string>("editable");
        using var host = new ViewHost(() => VStack(
            UI_Framework.UI.Button("Action", () => { }) with { ForegroundColor = color.Value },
            TextField(value) with { ForegroundColor = color.Value }));
        Layout(host, 400);
        var button = TestVisualTree.Find<Button>(host).Single();
        var input = TestVisualTree.Find<TextBox>(host).Single();
        Assert.AreEqual(Colors.Red, ((SolidColorBrush)button.Foreground).Color);
        Assert.AreEqual(Colors.Red, ((SolidColorBrush)input.Foreground).Color);
        ThemeStyles.Apply(host, new ThemeTokens { Ink = "#123456" });
        Layout(host, 400);
        Assert.AreEqual(Colors.Red, ((SolidColorBrush)button.Foreground).Color);
        color.Value = null;
        TestVisualTree.Flush();
        Assert.AreEqual(Color.FromRgb(18, 52, 86), ((SolidColorBrush)button.Foreground).Color);
        Assert.AreEqual(Color.FromRgb(18, 52, 86), ((SolidColorBrush)input.Foreground).Color);
    });

    [TestMethod]
    public void EmptyAdaptiveGridReleasesSpaceAndCanBePopulated() => StaTestRunner.Run(() =>
    {
        var populated = new State<bool>(false);
        using var host = new ViewHost(() => HStack(AdaptiveGrid(240, populated.Value ? [Text("item")] : []), Text("after")));
        Layout(host, 600);
        double Position() => TestVisualTree.Find<TextBlock>(host).Single(t => t.Text == "after").TranslatePoint(new Point(), host).X;
        Assert.AreEqual(0, Position(), 0.1);
        populated.Value = true;
        TestVisualTree.Flush(); Layout(host, 600);
        Assert.AreEqual(240, Position(), 0.1);
        populated.Value = false;
        TestVisualTree.Flush(); Layout(host, 600);
        Assert.AreEqual(0, Position(), 0.1);
    });

    [TestMethod]
    public void InvalidLayoutDimensionsAreRejected()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => AdaptiveGrid(0));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Text("x").Flex(double.NaN));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => FlexRow().Spacing(-1));
    }
}
