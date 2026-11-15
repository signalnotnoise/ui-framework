using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

[TestClass]
public sealed class AutomationLifetimeTests
{
    [DataTestMethod]
    [DataRow("row", 0)]
    [DataRow("list", 0)]
    [DataRow("editor", 1)]
    public void HeldPeerDoesNotRetainUnrelatedDisposedEditors(string kind, int expected) => StaTestRunner.Run(() =>
    {
        var (peer, weak) = DisposeWithHeldPeer(kind);
        Collect();
        Assert.AreEqual(expected, weak.Count(w => w.IsAlive));
        GC.KeepAlive(peer);
    });

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (AutomationPeer, WeakReference[]) DisposeWithHeldPeer(string kind)
    {
        using var host = new ViewHost(() => VirtualList(Enumerable.Range(0, 500)
            .Select(i => WpfUI.Native(() => new TextBox { Text = $"Editor {i}", Height = 30 })
                .Id($"row-{i}").AccessibilityLabel($"Record {i}")), 200));
        var window = new Window { Content = host, Width = 500, Height = 300, ShowActivated = false };
        try
        {
            window.Show(); TestVisualTree.Layout(host);
            TestAutomationTree.Initialize(window, "VirtualList");
            var list = UIElementAutomationPeer.CreatePeerForElement(TestVisualTree.Find<ItemsControl>(host).Single())!;
            var rows = list.GetChildren()!.Where(p => p.GetClassName() == "VirtualRow").ToArray();
            foreach (var row in rows) _ = row.GetChildren();
            var editors = TestVisualTree.Find<TextBox>(host).ToArray();
            Assert.IsTrue(editors.Length > 1);
            var held = kind == "list" ? list : kind == "row" ? rows[0]
                : UIElementAutomationPeer.CreatePeerForElement(editors[0])!;
            var weak = editors.Select(e => new WeakReference(e)).ToArray();
            host.Dispose(); window.Content = null;
            return (held, weak);
        }
        finally { window.Close(); }
    }

    [TestMethod]
    public void HeldRowPeerDropsRecycledChildrenAndDiscoversReplacementEditor() => StaTestRunner.Run(() =>
    {
        var ids = new StateList<int>(Enumerable.Range(0, 500));
        using var host = new ViewHost(() => VirtualList(ids.Select(i => WpfUI.Native(() =>
            new TextBox { Text = $"Editor {i}", Height = 30 }).Id($"row-{i}").AccessibilityLabel($"Record {i}")), 200));
        var window = new Window { Content = host, Width = 500, Height = 300, ShowActivated = false };
        try
        {
            window.Show(); TestVisualTree.Layout(host);
            TestAutomationTree.Initialize(window, "VirtualList");
            var list = UIElementAutomationPeer.CreatePeerForElement(TestVisualTree.Find<ItemsControl>(host).Single())!;
            var peer = list.GetChildren()!.Single(p => p.GetName() == "Record 0");
            Assert.AreEqual("Editor 0", EditorValue(peer));
            var scroll = TestVisualTree.Find<ScrollViewer>(host).First();
            for (var pass = 0; pass < 3; pass++)
            {
                scroll.ScrollToEnd(); TestVisualTree.Layout(host);
                Assert.IsTrue(peer.GetChildren() is not { Count: > 0 }, "An unrealized row must not expose a previous editor.");
                Assert.IsNotNull(peer.GetPattern(PatternInterface.VirtualizedItem));
                ((IVirtualizedItemProvider)peer.GetPattern(PatternInterface.VirtualizedItem)!).Realize();
                TestVisualTree.Layout(host);
                Assert.AreEqual("Editor 0", EditorValue(peer));
            }
            ids.Remove(0); TestVisualTree.Layout(host);
            Assert.IsTrue(peer.GetChildren() is not { Count: > 0 });
            ids.Add(0); ids.Move(ids.Count - 1, 0); TestVisualTree.Layout(host);
            list.ResetChildrenCache();
            var replacement = list.GetChildren()!.Single(p => p.GetName() == "Record 0");
            Assert.AreNotSame(peer, replacement);
            Assert.AreEqual("Editor 0", EditorValue(replacement));
            Assert.IsTrue(peer.GetChildren() is not { Count: > 0 }, "An old key's peer must not acquire the replacement row.");
            Assert.IsTrue(TestVisualTree.Find<TextBox>(host).Count() < 100);
        }
        finally { window.Close(); }
    });

    private static string? EditorValue(AutomationPeer peer)
    {
        if (peer.GetPattern(PatternInterface.Value) is IValueProvider value) return value.Value;
        foreach (var child in peer.GetChildren() ?? [])
            if (EditorValue(child) is { } text) return text;
        return null;
    }

    private static void Collect()
    {
        for (var i = 0; i < 3; i++)
        {
            TestVisualTree.Flush(); GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        }
    }
}
