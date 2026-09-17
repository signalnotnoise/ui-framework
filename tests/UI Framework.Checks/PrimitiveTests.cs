using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;
using System.Windows.Controls;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

[TestClass]
public sealed class PrimitiveTests
{
    [TestMethod]
    public void TextEditsWriteStateAndExternalChangesUpdateControl() => StaTestRunner.Run(() =>
    {
        var state = new State<string>("initial");
        using var host = new ViewHost(() => TextField(state));
        TestVisualTree.Layout(host);
        var input = TestVisualTree.Find<TextBox>(host).Single();
        input.Text = "edited";
        Assert.AreEqual("edited", state.Value);
        state.Value = "external";
        TestVisualTree.Flush();
        Assert.AreSame(input, TestVisualTree.Find<TextBox>(host).Single());
        Assert.AreEqual("external", input.Text);
    });

    [TestMethod]
    public void ButtonClickUpdatesItsRetainedControl() => StaTestRunner.Run(() =>
    {
        var count = new State<int>(0);
        using var host = new ViewHost(() => UI_Framework.UI.Button($"Count {count.Value}", () => count.Value++));
        TestVisualTree.Layout(host);
        var button = TestVisualTree.Find<Button>(host).Single();
        button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
        TestVisualTree.Flush();
        Assert.AreEqual(1, count.Value);
        Assert.AreSame(button, TestVisualTree.Find<Button>(host).Single());
        Assert.AreEqual("Count 1", button.Content);
    });

    [TestMethod]
    public void RetainedButtonInvokesLatestCallback() => StaTestRunner.Run(() =>
    {
        var version = new State<int>(1);
        var captured = 0;
        using var host = new ViewHost(() =>
        {
            var current = version.Value;
            return UI_Framework.UI.Button("Capture", () => captured = current);
        });
        TestVisualTree.Layout(host);
        var button = TestVisualTree.Find<Button>(host).Single();
        version.Value = 2;
        TestVisualTree.Flush();
        button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
        Assert.AreEqual(2, captured);
    });

    [TestMethod]
    public void KeyedPrimitiveMoveRetainsInputAndSelection() => StaTestRunner.Run(() =>
    {
        var reverse = new State<bool>(false);
        var state = new State<string>("editable text");
        using var host = new ViewHost(() => reverse.Value
            ? VStack(Text("label").Id("label"), TextField(state).Id("input"))
            : VStack(TextField(state).Id("input"), Text("label").Id("label")));
        TestVisualTree.Layout(host);
        var input = TestVisualTree.Find<TextBox>(host).Single();
        input.Select(2, 3);
        reverse.Value = true;
        TestVisualTree.Flush();
        Assert.AreSame(input, TestVisualTree.Find<TextBox>(host).Single());
        Assert.AreEqual(2, input.SelectionStart);
        Assert.AreEqual(3, input.SelectionLength);
    });

    [TestMethod]
    public void DuplicateSiblingKeysAreRejected() => StaTestRunner.Run(() =>
        Assert.ThrowsException<InvalidOperationException>(() =>
        {
            using var host = new ViewHost(() => VStack(Text("a").Id("same"), Text("b").Id("same")));
        }));
}
