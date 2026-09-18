using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;
using System.Windows.Controls;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

[TestClass]
public sealed class NavigationTests
{
    [TestMethod]
    public void HistoryHasDistinctEntryIdentitiesAndCannotPopRoot()
    {
        var history = new NavigationStack<string>("home");
        var root = history.Entries.Single().Id;
        var changes = 0;
        history.Changed += () => changes++;
        Assert.IsFalse(history.Back());
        history.Push("board");
        var first = history.Entries.Last().Id;
        history.Push("board");
        Assert.AreNotEqual(first, history.Entries.Last().Id);
        history.PopToRoot();
        Assert.AreEqual(root, history.Entries.Single().Id);
        history.Reset("home");
        Assert.AreNotEqual(root, history.Entries.Single().Id);
        Assert.AreEqual(4, changes);
        Assert.IsFalse(history.CanGoBack);
    }

    [TestMethod]
    public void NavigationRejectsCrossThreadAccessBeforeMutation()
    {
        var history = new NavigationStack<int>(0);
        Task.Run(() => Assert.ThrowsException<InvalidOperationException>(() => history.Push(1))).GetAwaiter().GetResult();
        Assert.AreEqual(1, history.Entries.Count);
    }

    [TestMethod]
    public void BackRestoresLocalStateAndHiddenScreensReleaseObservers() => StaTestRunner.Run(() =>
    {
        var history = new NavigationStack<int>(0);
        var probes = new Dictionary<int, VirtualizationProbe>();
        using var host = new ViewHost(() => Navigation(history, id => Component<VirtualizationProbe>(probe => probes[id] = probe)));
        TestVisualTree.Layout(host);
        var root = probes[0];
        root.Text.Value = "retained local value";
        history.Push(1);
        TestVisualTree.Layout(host);
        Assert.AreEqual(root.Mounts, root.Unmounts);
        var builds = root.Builds;
        root.Text.Value = "changed while hidden";
        TestVisualTree.Flush();
        Assert.AreEqual(builds, root.Builds);
        history.Back();
        TestVisualTree.Layout(host);
        Assert.AreSame(root, probes[0]);
        Assert.AreEqual("changed while hidden", TestVisualTree.Find<TextBox>(host).Single().Text);
        var popped = probes[1];
        history.Push(1);
        TestVisualTree.Layout(host);
        Assert.AreNotSame(popped, probes[1]);
        Assert.AreEqual("initial", TestVisualTree.Find<TextBox>(host).Single().Text);
        host.Dispose();
        Assert.AreEqual(root.Mounts, root.Unmounts);
        Assert.AreEqual(probes[1].Mounts, probes[1].Unmounts);
    });

    [TestMethod]
    public void BackRestoresVirtualRowStateIncludingRecycledRows() => StaTestRunner.Run(() =>
    {
        var history = new NavigationStack<int>(0);
        var probes = new Dictionary<int, VirtualizationProbe>();
        using var host = new ViewHost(() => Navigation(history, route => route == 0
            ? VirtualList(Enumerable.Range(0, 1000).Select(id => Component<VirtualizationProbe>(probe => probes[id] = probe).Id(id.ToString())), 300)
            : Text("details")));
        TestVisualTree.Layout(host);
        var first = probes[0];
        first.Text.Value = "saved virtual row";
        TestVisualTree.Find<ScrollViewer>(host).First().ScrollToEnd();
        TestVisualTree.Layout(host);
        Assert.AreEqual(first.Mounts, first.Unmounts);
        history.Push(1); TestVisualTree.Layout(host);
        history.Back(); TestVisualTree.Layout(host);
        Assert.AreSame(first, probes[0]);
        Assert.AreEqual("saved virtual row", TestVisualTree.Find<TextBox>(host).First().Text);
        Assert.IsTrue(TestVisualTree.Find<TextBox>(host).Count() < 40);
    });

    [TestMethod]
    public void NestedNavigationRetainsBothActiveAndHiddenScreenState() => StaTestRunner.Run(() =>
    {
        var outer = new NavigationStack<int>(0);
        var inner = new NavigationStack<int>(0);
        var probes = new Dictionary<int, VirtualizationProbe>();
        using var host = new ViewHost(() => Navigation(outer, route => route == 0
            ? Navigation(inner, id => Component<VirtualizationProbe>(probe => probes[id] = probe))
            : Text("outside")));
        TestVisualTree.Layout(host);
        var first = probes[0];
        first.Text.Value = "first saved";
        inner.Push(1); TestVisualTree.Layout(host);
        var second = probes[1];
        second.Text.Value = "second saved";
        outer.Push(1); TestVisualTree.Layout(host);
        Assert.AreEqual(first.Mounts, first.Unmounts);
        Assert.AreEqual(second.Mounts, second.Unmounts);
        outer.Back(); TestVisualTree.Layout(host);
        Assert.AreSame(second, probes[1]);
        Assert.AreEqual("second saved", TestVisualTree.Find<TextBox>(host).Single().Text);
        inner.Back(); TestVisualTree.Layout(host);
        Assert.AreSame(first, probes[0]);
        Assert.AreEqual("first saved", TestVisualTree.Find<TextBox>(host).Single().Text);
    });

    [TestMethod]
    public void RapidNavigationAndResetKeepOnlyTheCurrentScreen() => StaTestRunner.Run(() =>
    {
        var history = new NavigationStack<int>(0);
        using var host = new ViewHost(() => Navigation(history, id => Text($"Screen {id}"), NavigationTransition.None));
        var window = new Window { Content = host, Width = 500, Height = 300, ShowInTaskbar = false };
        try
        {
            window.Show();
            for (var i = 1; i <= 30; i++)
            {
                history.Push(i); TestVisualTree.Layout(host);
                Assert.AreEqual($"Screen {i}", TestVisualTree.Find<TextBlock>(host).Single().Text);
                Assert.IsTrue(TestVisualTree.Find<ViewHost>(host).All(view => !view.HasAnimatedProperties));
                history.Back(); TestVisualTree.Layout(host);
            }
            history.Reset(99); TestVisualTree.Layout(host);
            Assert.AreEqual("Screen 99", TestVisualTree.Find<TextBlock>(host).Single().Text);
            Assert.IsFalse(history.CanGoBack);
        }
        finally { window.Close(); }
    });
}
