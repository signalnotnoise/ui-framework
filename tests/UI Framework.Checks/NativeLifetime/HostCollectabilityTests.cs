using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

[TestClass]
public sealed class HostCollectabilityTests
{
    [TestMethod]
    public void SettledLateInitializationDoesNotRetainDisposedIslandThroughProviderTask() => StaTestRunner.Run(() =>
    {
        var pending = new TaskCompletionSource<NativeInitializationLease>(TaskCreationOptions.RunContinuationsAsynchronously);
        var weak = CreateDisposedAsyncIsland(pending);
        AssertCollected(weak);
        // The provider may retain its completed task/result. Its continuations
        // must no longer retain the disposed host or native control.
        GC.KeepAlive(pending);
    });

    [TestMethod]
    public void RepeatedDisposedHostsAndIslandsCollectWithPublisherStillAlive() => StaTestRunner.Run(() =>
    {
        var publisher = new State<int>(0);
        var weak = Enumerable.Range(0, 24).SelectMany(_ => CreateDisposedHost(publisher, true)).ToArray();
        AssertCollected(weak);
        publisher.Value++;
        TestVisualTree.Flush();
        GC.KeepAlive(publisher);
    });

    [TestMethod]
    public void MissingApplicationUnsubscribeIsDetectedByTheCollectabilityProbe() => StaTestRunner.Run(() =>
    {
        var publisher = new State<int>(0);
        var weak = CreateDisposedHost(publisher, false);
        Collect();
        Assert.IsTrue(weak[1].IsAlive, "A live publisher must expose the intentionally missing application cleanup.");
        ReleaseProbe(weak[1]);
        AssertCollected(weak);
        GC.KeepAlive(publisher);
    });

    [TestMethod]
    public void VisitedVirtualNativeRowsCollectAfterWorkspaceDisposal() => StaTestRunner.Run(() =>
    {
        var publisher = new State<int>(0);
        var weak = CreateDisposedVirtualWorkspace(publisher);
        Assert.IsTrue(weak.Length > 4, "The test must realize native rows before checking their collection.");
        AssertCollected(weak);
        GC.KeepAlive(publisher);
    });

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference[] CreateDisposedAsyncIsland(TaskCompletionSource<NativeInitializationLease> pending)
    {
        var errors = new List<Exception>();
        var control = new AsyncNativeTestControl(_ => pending.Task, errors.Add);
        var host = new ViewHost(() => WpfUI.Native(() => control, release: input => input.Dispose()));
        var weak = new[] { new WeakReference(host), new WeakReference(control) };
        host.Dispose();
        var lease = new NativeInitializationLease("late completion");
        pending.SetResult(lease);
        DispatcherTaskPump.Complete(control.Completion);
        Assert.AreEqual(1, lease.DisposeCount);
        Assert.AreEqual(0, errors.Count);
        return weak;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference[] CreateDisposedHost(State<int> publisher, bool unsubscribe)
    {
        var control = new SubscribedNativeTestControl(publisher);
        var host = new ViewHost(() =>
        {
            _ = publisher.Value;
            return WpfUI.Native(() => control, release: input => { if (unsubscribe) input.Dispose(); });
        });
        var weak = new[] { new WeakReference(host), new WeakReference(control), new WeakReference(control.Payload) };
        publisher.Value++; // Queue a renderer refresh immediately before disposal.
        host.Dispose(); host.Dispose();
        TestVisualTree.Flush();
        return weak;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference[] CreateDisposedVirtualWorkspace(State<int> publisher)
    {
        var weak = new List<WeakReference>();
        var host = new ViewHost(() =>
        {
            _ = publisher.Value;
            return VirtualList(Enumerable.Range(0, 200).Select(index => WpfUI.Native(() =>
            {
                var control = new SubscribedNativeTestControl(publisher) { Height = 30 };
                weak.Add(new(control)); weak.Add(new(control.Payload));
                return control;
            }, release: control => control.Dispose()).Id(index.ToString())), 180);
        });
        weak.Add(new(host));
        TestVisualTree.Layout(host);
        var initialReferences = weak.Count;
        var scroll = TestVisualTree.Find<ScrollViewer>(host).First();
        foreach (var offset in new[] { 600d, 1800d, 3600d, 0d })
        {
            scroll.ScrollToVerticalOffset(offset);
            TestVisualTree.Layout(host);
        }
        Assert.IsTrue(weak.Count > initialReferences, "Scrolling must visit and create additional native rows.");
        publisher.Value++;
        host.Dispose();
        TestVisualTree.Flush();
        return weak.ToArray();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ReleaseProbe(WeakReference weak) => ((SubscribedNativeTestControl)weak.Target!).Dispose();

    private static void AssertCollected(WeakReference[] weak)
    {
        for (var attempt = 0; attempt < 3; attempt++) Collect();
        Assert.AreEqual(0, weak.Count(reference => reference.IsAlive), "Disposed hosts, native controls and their payloads must be collectible after queued UI work drains.");
    }

    private static void Collect()
    {
        TestVisualTree.Flush();
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
    }
}
