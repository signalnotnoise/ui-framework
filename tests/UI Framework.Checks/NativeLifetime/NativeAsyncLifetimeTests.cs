using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows.Controls;
using UI_Framework.Wpf;
using static UI_Framework.UI;

[TestClass]
public sealed class NativeAsyncLifetimeTests
{
    [TestMethod]
    public void InitializationRetainsIdentityAndOriginalReleaseAcrossUpdates() => StaTestRunner.Run(() =>
    {
        var pending = new TaskCompletionSource<NativeInitializationLease>(TaskCreationOptions.RunContinuationsAsynchronously);
        var errors = new List<Exception>();
        var creates = 0;
        AsyncNativeTestControl? control = null;
        using var host = new ViewHost(() => WpfUI.Native(
            () => { creates++; return control = new(_ => pending.Task, errors.Add); },
            update: input => input.Tag = "current", release: input => input.Dispose()));
        host.Refresh();
        var lease = new NativeInitializationLease("ready");
        pending.SetResult(lease);
        DispatcherTaskPump.Complete(control!.Completion);
        host.Refresh();
        TestVisualTree.Layout(host);
        Assert.AreEqual(1, creates);
        Assert.AreSame(control, TestVisualTree.Find<AsyncNativeTestControl>(host).Single());
        Assert.AreEqual("ready", control.Text);
        Assert.AreEqual(0, lease.DisposeCount);
        host.Dispose(); host.Dispose();
        Assert.AreEqual(1, control.DisposeCount);
        Assert.AreEqual(1, lease.DisposeCount);
        Assert.AreEqual(0, errors.Count);
    });

    [TestMethod]
    public void OldInitializationCannotWriteIntoRemountedIsland() => StaTestRunner.Run(() =>
    {
        var oldPending = new TaskCompletionSource<NativeInitializationLease>(TaskCreationOptions.RunContinuationsAsynchronously);
        var newPending = new TaskCompletionSource<NativeInitializationLease>(TaskCreationOptions.RunContinuationsAsynchronously);
        var errors = new List<Exception>();
        var version = 0;
        AsyncNativeTestControl? current = null;
        using var host = new ViewHost(() => WpfUI.Native(
            () => current = new(_ => version == 0 ? oldPending.Task : newPending.Task, errors.Add),
            release: input => input.Dispose()).Id(version.ToString()));
        var previous = current!;
        version = 1;
        host.Refresh();
        var replacement = current!;
        Assert.AreNotSame(previous, replacement);
        var oldLease = new NativeInitializationLease("old document");
        var newLease = new NativeInitializationLease("new document");
        newPending.SetResult(newLease);
        DispatcherTaskPump.Complete(replacement.Completion);
        oldPending.SetResult(oldLease);
        DispatcherTaskPump.Complete(previous.Completion);
        Assert.AreEqual("", previous.Text);
        Assert.AreEqual("new document", replacement.Text);
        Assert.AreEqual(1, oldLease.DisposeCount);
        Assert.AreEqual(0, newLease.DisposeCount);
        host.Dispose();
        Assert.AreEqual(1, newLease.DisposeCount);
        Assert.AreEqual(0, errors.Count);
    });

    [TestMethod]
    public void CooperativeCancellationSettlesWithoutReportingAnInitializationFailure() => StaTestRunner.Run(() =>
    {
        var pending = new TaskCompletionSource<NativeInitializationLease>(TaskCreationOptions.RunContinuationsAsynchronously);
        var errors = new List<Exception>();
        AsyncNativeTestControl? control = null;
        using var host = new ViewHost(() => WpfUI.Native(
            () => control = new(_ => pending.Task, errors.Add), release: input => input.Dispose()));
        host.Dispose();
        pending.SetCanceled(control!.InitializationToken);
        DispatcherTaskPump.Complete(control.Completion);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(1, control.DisposeCount);
    });

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void CompletionAfterRemovalOrHostDisposalReleasesWithoutWritingDetachedControl(bool disposeHost) => StaTestRunner.Run(() =>
    {
        var pending = new TaskCompletionSource<NativeInitializationLease>(TaskCreationOptions.RunContinuationsAsynchronously);
        var errors = new List<Exception>();
        var shown = true;
        AsyncNativeTestControl? control = null;
        using var host = new ViewHost(() => VStack(shown
            ? [WpfUI.Native(() => control = new(_ => pending.Task, errors.Add), release: input => input.Dispose()).Id("editor")]
            : []));
        var removed = control!;
        if (disposeHost) host.Dispose();
        else { shown = false; host.Refresh(); }
        Assert.IsNull(removed.Parent);
        Assert.IsTrue(removed.InitializationToken.IsCancellationRequested);
        var lease = new NativeInitializationLease("must not appear");
        pending.SetResult(lease); // Deliberately ignores cancellation.
        DispatcherTaskPump.Complete(removed.Completion);
        Assert.AreEqual("", removed.Text);
        Assert.AreEqual(1, lease.DisposeCount);
        Assert.AreEqual(1, removed.DisposeCount);
        Assert.AreEqual(0, errors.Count);
    });

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void InitializationFaultIsObservedBeforeOrAfterRemoval(bool removeFirst) => StaTestRunner.Run(() =>
    {
        var pending = new TaskCompletionSource<NativeInitializationLease>(TaskCreationOptions.RunContinuationsAsynchronously);
        var errors = new List<Exception>();
        AsyncNativeTestControl? control = null;
        using var host = new ViewHost(() => WpfUI.Native(
            () => control = new(_ => pending.Task, errors.Add), release: input => input.Dispose()));
        if (removeFirst) host.Dispose();
        var failure = new InvalidOperationException("provider initialization failed");
        pending.SetException(failure);
        DispatcherTaskPump.Complete(control!.Completion);
        Assert.AreSame(failure, errors.Single());
        Assert.AreEqual("", control.Text);
        host.Dispose();
        Assert.AreEqual(1, control.DisposeCount);
    });

    [TestMethod]
    public void InitialUpdateFailureCancelsPendingInitializationAndReleasesLateResource() => StaTestRunner.Run(() =>
    {
        var pending = new TaskCompletionSource<NativeInitializationLease>(TaskCreationOptions.RunContinuationsAsynchronously);
        var errors = new List<Exception>();
        AsyncNativeTestControl? control = null;
        Assert.ThrowsException<InvalidOperationException>(() => new ViewHost(() => WpfUI.Native(
            () => control = new(_ => pending.Task, errors.Add),
            _ => throw new InvalidOperationException("partial setup failed"), input => input.Dispose())));
        Assert.IsTrue(control!.InitializationToken.IsCancellationRequested);
        Assert.IsNull(control.Parent);
        var lease = new NativeInitializationLease("late");
        pending.SetResult(lease);
        DispatcherTaskPump.Complete(control.Completion);
        Assert.AreEqual(1, lease.DisposeCount);
        Assert.AreEqual(1, control.DisposeCount);
        Assert.AreEqual(0, errors.Count);
    });

    [TestMethod]
    public void FactoryFailureLeavesUnreturnedResourcesApplicationOwnedAndReleasesAcceptedSibling() => StaTestRunner.Run(() =>
    {
        NativeInitializationLease? lease = null;
        var acceptedReleases = 0;
        var unacceptedReleases = 0;
        Assert.ThrowsException<InvalidOperationException>(() => new ViewHost(() => HStack(
            WpfUI.Native(() => new Border(), release: _ => acceptedReleases++),
            WpfUI.Native<Border>(() =>
            {
                lease = new NativeInitializationLease("allocated before factory failure");
                try { throw new InvalidOperationException("factory failed before returning"); }
                catch { lease.Dispose(); throw; }
            }, release: _ => unacceptedReleases++))));
        Assert.IsNotNull(lease);
        Assert.AreEqual(1, lease.DisposeCount);
        Assert.AreEqual(1, acceptedReleases);
        Assert.AreEqual(0, unacceptedReleases);
    });
}
