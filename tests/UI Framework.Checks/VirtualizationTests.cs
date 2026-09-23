using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

[TestClass]
public sealed class VirtualizationTests
{
    [TestMethod]
    public void RegenerationRestoresSecondEditorAndSelection() => StaTestRunner.Run(() =>
    {
        var first = new State<string>("first editor");
        var second = new State<string>("second editor selection");
        using var host = new ViewHost(() => VirtualList(
            [VStack(TextField(first), HStack(TextField(second))).Id("row")], 300));
        var window = new Window { Content = host, Width = 500, Height = 300, ShowInTaskbar = false };
        try
        {
            window.Show();
            TestVisualTree.Layout(host);
            var editor = TestVisualTree.Find<TextBox>(host).Last();
            Assert.IsTrue(editor.Focus());
            editor.Select(3, 5);
            TestVisualTree.Find<ItemsControl>(host).Single().Items.Refresh();
            TestVisualTree.Layout(host);
            var restored = TestVisualTree.Find<TextBox>(host).Last();
            Assert.AreNotSame(editor, restored);
            Assert.AreSame(restored, Keyboard.FocusedElement);
            Assert.AreEqual(3, restored.SelectionStart);
            Assert.AreEqual(5, restored.SelectionLength);
        }
        finally { window.Close(); }
    });

    [TestMethod]
    public void RegenerationUsesEditorKeysWhenSiblingsReorder() => StaTestRunner.Run(() =>
    {
        var first = new State<string>("first editor");
        var second = new State<string>("second editor");
        var reverse = false;
        using var host = new ViewHost(() => VirtualList([VStack(reverse
            ? [TextField(second).Id("second"), TextField(first).Id("first")]
            : [TextField(first).Id("first"), TextField(second).Id("second")]).Id("row")], 300));
        var window = new Window { Content = host, Width = 500, Height = 300, ShowInTaskbar = false };
        try
        {
            window.Show();
            TestVisualTree.Layout(host);
            var editor = TestVisualTree.Find<TextBox>(host).Last();
            Assert.IsTrue(editor.Focus());
            editor.Select(2, 4);
            TestVisualTree.Find<ItemsControl>(host).Single().Items.Refresh();
            reverse = true;
            host.Refresh();
            TestVisualTree.Layout(host);
            var restored = TestVisualTree.Find<TextBox>(host).Single(input => input.Text == second.Value);
            Assert.AreNotSame(editor, restored);
            Assert.AreSame(restored, Keyboard.FocusedElement);
            Assert.AreEqual(2, restored.SelectionStart);
            Assert.AreEqual(4, restored.SelectionLength);
        }
        finally { window.Close(); }
    });

    [TestMethod]
    public void QueuedFocusRestorationRespectsFocusMovedOutsideTheRow() => StaTestRunner.Run(() =>
    {
        var outside = new State<string>("outside");
        var inside = new State<string>("inside");
        using var host = new ViewHost(() => VStack(TextField(outside), VirtualList([TextField(inside).Id("row")], 180)));
        var window = new Window { Content = host, Width = 500, Height = 300, ShowInTaskbar = false };
        try
        {
            window.Show();
            TestVisualTree.Layout(host);
            Assert.IsTrue(TestVisualTree.Find<TextBox>(host).Last().Focus());
            TestVisualTree.Find<ItemsControl>(host).Single().Items.Refresh();
            host.UpdateLayout(); // Reattach synchronously, leaving the Input callback queued.
            var external = TestVisualTree.Find<TextBox>(host).First();
            Assert.IsTrue(external.Focus());
            TestVisualTree.Flush();
            Assert.AreSame(external, Keyboard.FocusedElement);
        }
        finally { window.Close(); }
    });

    [TestMethod]
    public void NativeRowsReleaseOnRecycleAndRecreateFromApplicationState() => StaTestRunner.Run(() =>
    {
        var created = 0;
        var released = 0;
        using var host = new ViewHost(() => VirtualList(Enumerable.Range(0, 1000).Select(id =>
            WpfUI.Native(() => { created++; return new TextBox { Text = $"row {id}", Height = 35 }; },
                release: element => { Assert.IsNull(element.Parent); released++; }).Id(id.ToString())), 300));
        TestVisualTree.Layout(host);
        var initial = TestVisualTree.Find<TextBox>(host).First();
        var scroll = TestVisualTree.Find<ScrollViewer>(host).First();
        scroll.ScrollToEnd();
        TestVisualTree.Layout(host);
        Assert.IsTrue(released > 0);
        scroll.ScrollToHome();
        TestVisualTree.Layout(host);
        var restored = TestVisualTree.Find<TextBox>(host).First();
        Assert.AreNotSame(initial, restored);
        Assert.AreEqual("row 0", restored.Text);
        host.Dispose();
        Assert.AreEqual(created, released);
    });

    [TestMethod]
    public void ThrowingRowCleanupStillReleasesEveryRealizedIsland() => StaTestRunner.Run(() =>
    {
        var created = 0;
        var released = 0;
        using var host = new ViewHost(() => VirtualList(Enumerable.Range(0, 10).Select(id =>
            WpfUI.Native(() => { created++; return new TextBox { Height = 35 }; },
                release: _ => { released++; throw new InvalidOperationException("release"); }).Id(id.ToString())), 300));
        TestVisualTree.Layout(host);
        Assert.IsTrue(created > 1);
        Assert.ThrowsException<AggregateException>(host.Dispose);
        Assert.AreEqual(created, released);
        host.Dispose();
        Assert.AreEqual(created, released);
    });

    private static ViewHost Create(StateList<int> ids, Dictionary<int, VirtualizationProbe> probes) => new(() =>
        VirtualList(ids.Select(id => Component<VirtualizationProbe>(probe =>
        {
            probe.Id = id;
            probes[id] = probe;
        }).Id(id.ToString()).Memo(id)), 300));

    [TestMethod]
    public void ThousandRowsMountOnlyViewportAndBuffer() => StaTestRunner.Run(() =>
    {
        var ids = new StateList<int>(Enumerable.Range(0, 1000));
        var probes = new Dictionary<int, VirtualizationProbe>();
        using var host = Create(ids, probes);
        TestVisualTree.Layout(host);
        Assert.IsTrue(probes.Count > 0 && probes.Count < 40, $"Mounted {probes.Count} rows");
        Assert.IsTrue(TestVisualTree.Find<TextBox>(host).Count() < 40);
        host.Dispose();
        Assert.IsTrue(probes.Values.All(p => p.Mounts == p.Unmounts));
    });

    [TestMethod]
    public void OffscreenInsertAndRemovalRetainVisibleInputs() => StaTestRunner.Run(() =>
    {
        var ids = new StateList<int>(Enumerable.Range(0, 1000));
        var probes = new Dictionary<int, VirtualizationProbe>();
        using var host = Create(ids, probes);
        TestVisualTree.Layout(host);
        var first = TestVisualTree.Find<TextBox>(host).First();
        first.Text = "retained selection";
        first.Select(2, 4);
        ids.Add(1000);
        TestVisualTree.Layout(host);
        ids.Remove(999);
        TestVisualTree.Layout(host);
        Assert.AreSame(first, TestVisualTree.Find<TextBox>(host).First());
        Assert.AreEqual(2, first.SelectionStart);
        Assert.AreEqual(4, first.SelectionLength);
        Assert.AreEqual(1, probes[0].Mounts);
        Assert.AreEqual(0, probes[0].Unmounts);
    });

    [TestMethod]
    public void MovingOffscreenRowRetainsVisibleInputs() => StaTestRunner.Run(() =>
    {
        var ids = new StateList<int>(Enumerable.Range(0, 1000));
        var probes = new Dictionary<int, VirtualizationProbe>();
        using var host = Create(ids, probes);
        TestVisualTree.Layout(host);
        var first = TestVisualTree.Find<TextBox>(host).First();
        ids.Move(900, 950);
        TestVisualTree.Layout(host);
        Assert.AreSame(first, TestVisualTree.Find<TextBox>(host).First());
        Assert.AreEqual(1, probes[0].Mounts);
    });

    [TestMethod]
    public void ScrollingRestoresLocalStateAndReleasesObservers() => StaTestRunner.Run(() =>
    {
        var ids = new StateList<int>(Enumerable.Range(0, 1000));
        var probes = new Dictionary<int, VirtualizationProbe>();
        using var host = Create(ids, probes);
        TestVisualTree.Layout(host);
        var original = probes[0];
        original.Text.Value = "saved edit";
        original.Expanded.Value = true;
        TestVisualTree.Layout(host);
        var scroll = TestVisualTree.Find<ScrollViewer>(host).First();
        scroll.ScrollToEnd();
        TestVisualTree.Layout(host);
        Assert.AreEqual(original.Mounts, original.Unmounts);
        var builds = original.Builds;
        original.Text.Value = "offscreen edit";
        TestVisualTree.Flush();
        Assert.AreEqual(builds, original.Builds);
        Assert.IsTrue(probes.Values.Count(p => p.Mounts > p.Unmounts) < 40);
        scroll.ScrollToHome();
        TestVisualTree.Layout(host);
        Assert.AreSame(original, probes[0]);
        Assert.IsTrue(original.Expanded.Value);
        Assert.AreEqual("offscreen edit", TestVisualTree.Find<TextBox>(host).First().Text);
    });

    [TestMethod]
    public void RealWindowRetainsFocusAfterScrollRoundTrip() => StaTestRunner.Run(() =>
    {
        var ids = new StateList<int>(Enumerable.Range(0, 1000));
        var probes = new Dictionary<int, VirtualizationProbe>();
        using var host = Create(ids, probes);
        var window = new Window { Content = host, Width = 500, Height = 300, ShowInTaskbar = false };
        try
        {
            window.Show();
            TestVisualTree.Layout(host);
            var original = TestVisualTree.Find<TextBox>(host).First();
            Assert.IsTrue(original.Focus());
            Assert.AreSame(original, Keyboard.FocusedElement);

            var scroll = TestVisualTree.Find<ScrollViewer>(host).First();
            scroll.ScrollToEnd();
            TestVisualTree.Layout(host);
            scroll.ScrollToHome();
            TestVisualTree.Layout(host);

            var restored = TestVisualTree.Find<TextBox>(host).First();
            Assert.AreSame(restored, Keyboard.FocusedElement);
            Assert.AreEqual("initial", restored.Text);
        }
        finally
        {
            window.Close();
        }
    });

    [TestMethod]
    public void RealWindowRestoresFocusAfterContainerRegeneration() => StaTestRunner.Run(() =>
    {
        var ids = new StateList<int>(Enumerable.Range(0, 1000));
        var probes = new Dictionary<int, VirtualizationProbe>();
        using var host = Create(ids, probes);
        var window = new Window { Content = host, Width = 500, Height = 300, ShowInTaskbar = false };
        try
        {
            window.Show();
            TestVisualTree.Layout(host);
            var probe = probes[0];
            probe.Text.Value = "focused row";
            TestVisualTree.Layout(host);
            var original = TestVisualTree.Find<TextBox>(host).Single(input => input.Text == "focused row");
            Assert.IsTrue(original.Focus());
            Assert.AreSame(original, Keyboard.FocusedElement);
            var mounts = probe.Mounts;
            var unmounts = probe.Unmounts;

            // Refresh WPF's containers while preserving the framework's keyed rows.
            TestVisualTree.Find<ItemsControl>(host).Single().Items.Refresh();
            TestVisualTree.Layout(host);

            var restored = TestVisualTree.Find<TextBox>(host).Single(input => input.Text == "focused row");
            Assert.AreNotSame(original, restored, "Container regeneration must recreate the native input.");
            Assert.AreSame(probe, probes[0]);
            Assert.AreEqual(unmounts + 1, probe.Unmounts);
            Assert.AreEqual(mounts + 1, probe.Mounts);
            Assert.AreSame(restored, Keyboard.FocusedElement);
            restored.Text = "edited after regeneration";
            Assert.AreEqual("edited after regeneration", probe.Text.Value);
            Assert.AreEqual("initial", probes[1].Text.Value);
        }
        finally
        {
            window.Close();
        }
    });

    [TestMethod]
    public void VisibleMoveRetainsUnaffectedRowControls() => StaTestRunner.Run(() =>
    {
        var ids = new StateList<int>(Enumerable.Range(0, 1000));
        var probes = new Dictionary<int, VirtualizationProbe>();
        using var host = Create(ids, probes);
        TestVisualTree.Layout(host);
        probes[0].Text.Value = "moved row";
        probes[2].Text.Value = "unaffected row";
        TestVisualTree.Layout(host);
        var unaffected = TestVisualTree.Find<TextBox>(host).Single(input => input.Text == "unaffected row");
        unaffected.Select(1, 3);
        var moved = probes[0];
        ids.Move(0, 1);
        TestVisualTree.Layout(host);
        Assert.AreSame(unaffected, TestVisualTree.Find<TextBox>(host).Single(input => input.Text == "unaffected row"));
        Assert.AreEqual(1, unaffected.SelectionStart);
        Assert.AreEqual(3, unaffected.SelectionLength);
        Assert.AreSame(moved, probes[0]);
        var input = TestVisualTree.Find<TextBox>(host).Single(field => field.Text == "moved row");
        input.Text = "correct binding";
        Assert.AreEqual("correct binding", moved.Text.Value);
        Assert.AreEqual("unaffected row", probes[2].Text.Value);
    });

    [TestMethod]
    public void RemovedKeyGetsFreshStateOnReinsertion() => StaTestRunner.Run(() =>
    {
        var ids = new StateList<int>(Enumerable.Range(0, 1000));
        var probes = new Dictionary<int, VirtualizationProbe>();
        using var host = Create(ids, probes);
        TestVisualTree.Layout(host);
        var original = probes[0];
        original.Text.Value = "discard me";
        ids.Remove(0);
        TestVisualTree.Layout(host);
        ids.ReplaceAll(new[] { 0 }.Concat(ids));
        TestVisualTree.Layout(host);
        Assert.AreNotSame(original, probes[0]);
        Assert.AreEqual("initial", probes[0].Text.Value);
    });

    [TestMethod]
    public void VirtualRowsRequireUniqueKeys() => StaTestRunner.Run(() =>
    {
        Assert.ThrowsException<InvalidOperationException>(() =>
        {
            using var host = new ViewHost(() => VirtualList([Text("no key")], 300));
        });
        Assert.ThrowsException<InvalidOperationException>(() =>
        {
            using var host = new ViewHost(() => VirtualList([Text("a").Id("same"), Text("b").Id("same")], 300));
        });
    });
}
