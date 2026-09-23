using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows.Controls;
using UI_Framework.Wpf;
using static UI_Framework.UI;
using UI_Framework;
using System.Windows;

[TestClass]
public sealed class RendererFailureTests
{
    [TestMethod]
    public void FailedComponentMountLeavesHostRecoverable() => StaTestRunner.Run(() =>
    {
        ThrowOnMounted.ShouldThrow = false;
        ThrowOnMounted.Unmounts = 0;
        var showComponent = false;
        using var host = new ViewHost(() => showComponent ? Component<ThrowOnMounted>() : Text("stable"));
        showComponent = true;
        ThrowOnMounted.ShouldThrow = true;
        Assert.ThrowsException<InvalidOperationException>(host.Refresh);
        Assert.AreEqual(1, ThrowOnMounted.Unmounts);
        ThrowOnMounted.ShouldThrow = false;
        host.Refresh();
        Assert.IsTrue(host.Content is System.Windows.Controls.Border);
        Assert.AreEqual(1, ThrowOnMounted.Unmounts);
    });

    [TestMethod]
    public void FailedPanelPatchLeavesHostRecoverable() => StaTestRunner.Run(() =>
    {
        var fail = false;
        using var host = new ViewHost(() => fail
            ? VStack(Text("updated"), Text("bad").Background("not-a-color"))
            : VStack(Text("stable"), Text("second")));
        fail = true;
        Assert.ThrowsException<FormatException>(host.Refresh);
        Assert.IsNull(host.Content);
        fail = false;
        host.Refresh();
        TestVisualTree.Layout(host);
        CollectionAssert.AreEqual(new[] { "stable", "second" }, TestVisualTree.Find<TextBlock>(host).Select(text => text.Text).ToArray());
    });

    [TestMethod]
    public void FailedBrushPatchLeavesHostRecoverable() => StaTestRunner.Run(() =>
    {
        var fail = false;
        using var host = new ViewHost(() => fail ? Text("bad").Background("not-a-color") : Text("stable"));
        fail = true;
        Assert.ThrowsException<FormatException>(host.Refresh);
        Assert.IsNull(host.Content);
        fail = false;
        host.Refresh();
        TestVisualTree.Layout(host);
        Assert.AreEqual("stable", TestVisualTree.Find<TextBlock>(host).Single().Text);
    });

    [TestMethod]
    public void FailedNativeUpdateLeavesHostRecoverable() => StaTestRunner.Run(() =>
    {
        var fail = false;
        using var host = new ViewHost(() => WpfUI.Native(
            () => new TextBox(),
            _ => { if (fail) throw new InvalidOperationException("native update failed"); }).Id("native"));
        fail = true;
        Assert.ThrowsException<InvalidOperationException>(host.Refresh);
        fail = false;
        host.Refresh();
        Assert.IsNotNull(host.Content);
    });

    [TestMethod]
    public void FailedPatchDetachesCallbacksAndRequiresExplicitRecovery() => StaTestRunner.Run(() =>
    {
        var state = new State<int>(0);
        var fail = false;
        var builds = 0;
        var clicks = 0;
        var releases = 0;
        using var host = new ViewHost(() =>
        {
            builds++;
            return VStack(Button(state.Value.ToString(), () => clicks++),
                WpfUI.Native(() => new TextBox(), input =>
                {
                    input.Text = "mutation before failure";
                    if (fail) throw new InvalidOperationException("update");
                }, _ => releases++));
        });
        TestVisualTree.Layout(host);
        var staleButton = TestVisualTree.Find<Button>(host).Single();
        fail = true;
        Assert.ThrowsException<InvalidOperationException>(host.Refresh);
        Assert.IsNull(host.Content);
        Assert.AreEqual(1, releases);
        staleButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
        Assert.AreEqual(0, clicks);
        var failedBuilds = builds;
        state.Value++;
        TestVisualTree.Flush();
        Assert.AreEqual(failedBuilds, builds);
        fail = false;
        host.Refresh();
        TestVisualTree.Layout(host);
        Assert.AreEqual("1", TestVisualTree.Find<Button>(host).Single().Content);
        state.Value++;
        TestVisualTree.Layout(host);
        Assert.AreEqual("2", TestVisualTree.Find<Button>(host).Single().Content);
        host.Dispose();
        Assert.AreEqual(2, releases);
    });

    [TestMethod]
    public void MountAndCleanupFailuresAreBothReported() => StaTestRunner.Run(() =>
    {
        var failure = Assert.ThrowsException<AggregateException>(() => new ViewHost(() => Component<FailingLifecycleComponent>()));
        var errors = failure.Flatten().InnerExceptions;
        Assert.AreEqual(2, errors.Count);
        Assert.IsTrue(errors.Any(error => error.Message == "mount failure"));
        Assert.IsTrue(errors.Any(error => error.Message == "unmount failure"));
    });

    [TestMethod]
    public void FailedBodyBuildRetainsTheGoodNativeTree() => StaTestRunner.Run(() =>
    {
        var fail = false;
        var state = new State<string>("stable");
        using var host = new ViewHost(() => fail ? throw new InvalidOperationException("body") : TextField(state));
        TestVisualTree.Layout(host);
        var input = TestVisualTree.Find<TextBox>(host).Single();
        fail = true;
        Assert.ThrowsException<InvalidOperationException>(host.Refresh);
        Assert.AreSame(input, TestVisualTree.Find<TextBox>(host).Single());
        Assert.AreEqual("stable", input.Text);
        fail = false;
        state.Value = "recovered through subscription";
        TestVisualTree.Layout(host);
        Assert.AreSame(input, TestVisualTree.Find<TextBox>(host).Single());
        Assert.AreEqual(state.Value, input.Text);
    });

    [TestMethod]
    public void ChildBodyFailureAfterSiblingPatchClearsParentAndUnmountsChild() => StaTestRunner.Run(() =>
    {
        var fail = false;
        FailingBodyComponent? component = null;
        using var host = new ViewHost(() => VStack(Text(fail ? "updated" : "stable"),
            Component<FailingBodyComponent>(instance => { component = instance; instance.Fail = fail; })));
        var original = component!;
        fail = true;
        Assert.ThrowsException<InvalidOperationException>(host.Refresh);
        Assert.IsNull(host.Content);
        Assert.AreEqual(1, original.Unmounts);
        fail = false;
        host.Refresh();
        Assert.AreNotSame(original, component);
        TestVisualTree.Layout(host);
        CollectionAssert.AreEqual(new[] { "stable", "child" }, TestVisualTree.Find<TextBlock>(host).Select(text => text.Text).ToArray());
    });

    [TestMethod]
    public void RemovalFailureStillReleasesUncommittedNewSiblings() => StaTestRunner.Run(() =>
    {
        var replace = false;
        var newReleases = 0;
        var oldReleases = 0;
        using var host = new ViewHost(() => replace
            ? VStack(WpfUI.Native(() => new TextBox(), release: _ => newReleases++).Id("new"))
            : VStack(WpfUI.Native(() => new TextBox(), release: _ =>
            { oldReleases++; throw new InvalidOperationException("old release"); }).Id("old")));
        replace = true;
        Assert.ThrowsException<AggregateException>(host.Refresh);
        Assert.IsNull(host.Content);
        Assert.AreEqual(1, oldReleases);
        Assert.AreEqual(1, newReleases);
        host.Refresh();
        host.Dispose();
        Assert.AreEqual(2, newReleases);
        Assert.AreEqual(1, oldReleases);
    });

    [TestMethod]
    public void LaterSiblingFailureReleasesReplacementsAndOriginalsExactlyOnce() => StaTestRunner.Run(() =>
    {
        var fail = false;
        var releases = 0;
        var creations = 0;
        View Island(string key, bool throws = false) => WpfUI.Native(
            () => { creations++; return new TextBox(); },
            _ => { if (throws) throw new InvalidOperationException("later child"); },
            _ => releases++).Id(key);
        using var host = new ViewHost(() => fail
            ? VStack(Island("replacement"), Island("failure", true))
            : VStack(Island("original"), Text("stable")));
        fail = true;
        Assert.ThrowsException<InvalidOperationException>(host.Refresh);
        Assert.IsNull(host.Content);
        Assert.AreEqual(3, creations);
        Assert.AreEqual(creations, releases);
        fail = false;
        host.Refresh();
        host.Dispose();
        Assert.AreEqual(creations, releases);
    });
}
