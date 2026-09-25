using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

[TestClass]
public sealed class WorkspaceLayoutTests
{
    [TestMethod]
    public void FlexRowUpdatesSizingAndGapsAcrossReorderAndRemoval() => StaTestRunner.Run(() =>
    {
        var phase = new State<int>(0);
        var value = new State<string>("retained editor");
        using var host = new ViewHost(() => FlexRow(phase.Value switch
        {
            0 => [TextEditor(value).Id("editor"), Text("fixed").Width(100).Id("fixed")],
            1 => [Text("fixed").Width(80).Id("fixed"), TextEditor(value).Flex(2).Id("editor")],
            2 => [TextEditor(value).Id("editor")],
            _ => []
        }).Spacing(phase.Value == 0 ? 10 : 20));
        TestVisualTree.Layout(host);
        var editor = TestVisualTree.Find<TextBox>(host).Single();
        editor.Select(2, 4);
        Assert.AreEqual(390, editor.ActualWidth, 0.1);
        phase.Value = 1; TestVisualTree.Layout(host);
        Assert.AreSame(editor, TestVisualTree.Find<TextBox>(host).Single());
        Assert.AreEqual(400, editor.ActualWidth, 0.1);
        Assert.AreEqual(100, editor.TranslatePoint(new Point(), host).X, 0.1);
        Assert.AreEqual(4, editor.SelectionLength);
        phase.Value = 2; TestVisualTree.Layout(host);
        Assert.AreEqual(500, editor.ActualWidth, 0.1);
        phase.Value = 3; TestVisualTree.Layout(host);
        Assert.AreEqual(0, TestVisualTree.Find<TextBox>(host).Count());
    });

    [TestMethod]
    public void DockFillsCenterAndRetainsItWhenAnEdgeIsRemoved() => StaTestRunner.Run(() =>
    {
        var left = new State<bool>(true);
        var contents = new State<string>("center");
        using var host = new ViewHost(() => Dock(TextEditor(contents), top: Text("top").Height(40), bottom: Text("bottom").Height(20),
            left: left.Value ? Text("left").Width(100) : null, right: Text("right").Width(50)));
        TestVisualTree.Layout(host);
        var editor = TestVisualTree.Find<TextBox>(host).Single();
        Assert.AreEqual(350, editor.ActualWidth, 0.1); Assert.AreEqual(240, editor.ActualHeight, 0.1);
        left.Value = false; TestVisualTree.Layout(host);
        Assert.AreSame(editor, TestVisualTree.Find<TextBox>(host).Single());
        Assert.AreEqual(450, editor.ActualWidth, 0.1);
    });

    [TestMethod]
    public void FlexColumnAllocatesRemainingHeightAndRetainsEditors() => StaTestRunner.Run(() =>
    {
        var text = new State<string>("retained editor");
        var header = new State<double>(40);
        using var host = new ViewHost(() => FlexColumn(Text("Header").Height(header.Value),
            TextEditor(text).Id("editor"), Text("Footer").Height(20)).Spacing(10));
        TestVisualTree.Layout(host);
        var editor = TestVisualTree.Find<TextBox>(host).Single();
        Assert.AreEqual(220, editor.ActualHeight, 0.1);
        editor.Select(2, 4);
        header.Value = 60; TestVisualTree.Layout(host);
        Assert.AreSame(editor, TestVisualTree.Find<TextBox>(host).Single());
        Assert.AreEqual(200, editor.ActualHeight, 0.1);
        Assert.AreEqual(4, editor.SelectionLength);
        host.Measure(new Size(500, double.PositiveInfinity));
        Assert.IsTrue(double.IsFinite(host.DesiredSize.Height));
    });

    [TestMethod]
    public void FlexColumnHonorsWeightsAutoHeightAndReorder() => StaTestRunner.Run(() =>
    {
        var reverse = new State<bool>(false);
        using var host = new ViewHost(() => FlexColumn(reverse.Value
            ? [Text("two").Flex(2).Id("two"), Text("one").Id("one")]
            : [Text("one").Id("one"), Text("two").Flex(2).Id("two")]).Spacing(0));
        TestVisualTree.Layout(host);
        var one = TestVisualTree.Find<TextBlock>(host).Single(t => t.Text == "one");
        var two = TestVisualTree.Find<TextBlock>(host).Single(t => t.Text == "two");
        Assert.AreEqual(100, one.ActualHeight, 0.1); Assert.AreEqual(200, two.ActualHeight, 0.1);
        reverse.Value = true; TestVisualTree.Layout(host);
        Assert.AreSame(one, TestVisualTree.Find<TextBlock>(host).Single(t => t.Text == "one"));
        Assert.AreEqual(200, one.TranslatePoint(new Point(), host).Y, 0.1);
    });

    [TestMethod]
    public void SplitRetainsBothNativePanesAcrossCollapseOrientationAndReorder() => StaTestRunner.Run(() =>
    {
        var extent = new State<double>(140);
        var collapsed = new State<bool>(false);
        var vertical = new State<bool>(false);
        var reverse = new State<bool>(false);
        var creates = 0; var releases = 0;
        View Pane(string key) => WpfUI.Native(() => { creates++; return new TextBox { Text = key }; },
            release: _ => releases++).Id(key);
        using var host = new ViewHost(() => SplitPane(Pane(reverse.Value ? "two" : "one"), Pane(reverse.Value ? "one" : "two"),
            extent, vertical.Value ? SplitAxis.Vertical : SplitAxis.Horizontal, 60, 60, collapsed.Value));
        TestVisualTree.Layout(host);
        var one = TestVisualTree.Find<TextBox>(host).Single(t => t.Text == "one");
        one.Select(1, 2);
        Assert.AreEqual(140, one.ActualWidth, 0.1);
        collapsed.Value = true; TestVisualTree.Layout(host);
        Assert.AreEqual(2, creates); Assert.AreEqual(0, releases);
        collapsed.Value = false; vertical.Value = true; extent.Value = 100; TestVisualTree.Layout(host);
        Assert.AreSame(one, TestVisualTree.Find<TextBox>(host).Single(t => t.Text == "one"));
        Assert.AreEqual(100, one.ActualHeight, 0.1); Assert.AreEqual(2, one.SelectionLength);
        reverse.Value = true; TestVisualTree.Layout(host);
        Assert.AreEqual(106, one.TranslatePoint(new Point(), host).Y, 0.1);
        Assert.AreEqual(2, creates);
        host.Dispose(); Assert.AreEqual(2, releases);
    });

    [TestMethod]
    public void SplitterMouseAndKeyboardResizeRespectBoundsAndBinding() => StaTestRunner.Run(() =>
    {
        var extent = new State<double>(140);
        using var host = new ViewHost(() => SplitPane(Text("one"), Text("two"), extent, minimumFirst: 80, minimumSecond: 100));
        using var source = new HwndSource(new HwndSourceParameters("Splitter input regression")
        { Width = 500, Height = 300, PositionX = -10000, PositionY = -10000, WindowStyle = unchecked((int)0x80000000) });
        source.RootVisual = host;
        TestVisualTree.Layout(host);
        var splitter = TestVisualTree.Find<GridSplitter>(host).Single();
        Assert.IsTrue(splitter.Focusable);
        Assert.AreEqual("Resize panes", System.Windows.Automation.AutomationProperties.GetName(splitter));
        splitter.RaiseEvent(new DragStartedEventArgs(0, 0) { RoutedEvent = Thumb.DragStartedEvent });
        splitter.RaiseEvent(new DragDeltaEventArgs(30, 0) { RoutedEvent = Thumb.DragDeltaEvent });
        TestVisualTree.Layout(host);
        splitter.RaiseEvent(new DragCompletedEventArgs(30, 0, false) { RoutedEvent = Thumb.DragCompletedEvent });
        TestVisualTree.Layout(host);
        Assert.AreEqual(170, extent.Value, 0.1);
        splitter.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Right) { RoutedEvent = Keyboard.KeyDownEvent });
        TestVisualTree.Layout(host);
        splitter.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Right) { RoutedEvent = Keyboard.KeyUpEvent });
        TestVisualTree.Layout(host);
        Assert.AreEqual(180, extent.Value, 0.1);
        splitter.RaiseEvent(new DragStartedEventArgs(0, 0) { RoutedEvent = Thumb.DragStartedEvent });
        splitter.RaiseEvent(new DragDeltaEventArgs(-1000, 0) { RoutedEvent = Thumb.DragDeltaEvent });
        TestVisualTree.Layout(host);
        splitter.RaiseEvent(new DragCompletedEventArgs(-1000, 0, false) { RoutedEvent = Thumb.DragCompletedEvent });
        Assert.AreEqual(80, extent.Value, 0.1);
    });

    [TestMethod]
    public void SplitResizeCanBeRejectedAndCanceledWithoutChangingTheModel() => StaTestRunner.Run(() =>
    {
        var writes = 0;
        var binding = new UI_Framework.Binding<double>(() => 120, _ => writes++);
        using var host = new ViewHost(() => SplitPane(Text("one"), Text("two"), binding));
        using var source = new HwndSource(new HwndSourceParameters("Splitter cancellation regression")
        { Width = 500, Height = 300, PositionX = -10000, PositionY = -10000, WindowStyle = unchecked((int)0x80000000) });
        source.RootVisual = host;
        TestVisualTree.Layout(host);
        var splitter = TestVisualTree.Find<GridSplitter>(host).Single();
        var panel = (Grid)splitter.Parent;
        splitter.RaiseEvent(new DragStartedEventArgs(0, 0) { RoutedEvent = Thumb.DragStartedEvent });
        splitter.RaiseEvent(new DragDeltaEventArgs(30, 0) { RoutedEvent = Thumb.DragDeltaEvent });
        TestVisualTree.Layout(host);
        splitter.RaiseEvent(new DragCompletedEventArgs(30, 0, false) { RoutedEvent = Thumb.DragCompletedEvent });
        TestVisualTree.Layout(host);
        Assert.AreEqual(1, writes); Assert.AreEqual(120, panel.ColumnDefinitions[0].ActualWidth, 0.1);
        splitter.RaiseEvent(new DragStartedEventArgs(0, 0) { RoutedEvent = Thumb.DragStartedEvent });
        splitter.RaiseEvent(new DragDeltaEventArgs(30, 0) { RoutedEvent = Thumb.DragDeltaEvent });
        TestVisualTree.Layout(host);
        // Routed DragStarted initializes GridSplitter, but does not set Thumb.IsDragging.
        // Escape exercises GridSplitter's native cancellation path in this event harness.
        splitter.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Escape) { RoutedEvent = Keyboard.KeyDownEvent });
        TestVisualTree.Layout(host);
        Assert.AreEqual(1, writes); Assert.AreEqual(120, panel.ColumnDefinitions[0].ActualWidth, 0.1);
        host.Dispose();
        splitter.RaiseEvent(new DragCompletedEventArgs(0, 0, false) { RoutedEvent = Thumb.DragCompletedEvent });
        Assert.AreEqual(1, writes);
    });

    [TestMethod]
    public void InvalidSplitConfigurationPreservesExistingTree() => StaTestRunner.Run(() =>
    {
        var extent = new State<double>(140);
        var view = SplitPane(Text("one"), Text("two"), extent);
        using var host = new ViewHost(() => view);
        var original = host.Content;
        var valid = view;
        foreach (var bad in new[] { valid with { Children = [Text("only")] }, valid with { SplitLayout = null },
            valid with { SplitLayout = valid.SplitLayout! with { FirstExtent = double.NaN } },
            valid with { SplitLayout = valid.SplitLayout! with { MinimumSecond = -1 } },
            valid with { SplitLayout = valid.SplitLayout! with { Axis = (SplitAxis)99 } } })
        {
            view = bad; Assert.ThrowsException<InvalidOperationException>(host.Refresh); Assert.AreSame(original, host.Content);
        }
    });
}
