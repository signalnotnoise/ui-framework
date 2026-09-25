using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace Lab_Feedback_WPF.Services;

// Experimental application-owned policy. Not part of the framework/package API.
// Closing drains pending cleanup; it cannot reverse the thread-wide CLR setting.
internal sealed class ApplicationComCleanupPolicy
{
    [ThreadStatic] private static bool installed;
    private readonly Dispatcher dispatcher;
    private readonly Action cleanup;
    private readonly Action<Exception>? reportFailure;
    private DispatcherOperation? pending;
    private DispatcherOperation? cleanupOperation;
    private int updateDepth;
    private bool requested, draining, closing, closed;

    internal ApplicationComCleanupPolicy(Dispatcher dispatcher, Action? cleanup = null, Action<Exception>? reportFailure = null)
    {
        dispatcher.VerifyAccess();
        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA || installed)
            throw new InvalidOperationException("Requires an application-owned STA thread with one cleanup policy.");
        this.dispatcher = dispatcher;
        this.cleanup = cleanup ?? Marshal.CleanupUnusedObjectsInCurrentContext;
        this.reportFailure = reportFailure;
        Thread.CurrentThread.DisableComObjectEagerCleanup();
        installed = true;
        dispatcher.Hooks.OperationCompleted += OnOperationCompleted;
    }

    internal int CleanupCount { get; private set; }
    internal Exception? LastError { get; private set; }
    internal int FailureCount { get; private set; }
    internal bool IsClosed => closed;

    internal void RunUpdate(Action action)
    {
        dispatcher.VerifyAccess();
        if (closing || closed) throw new InvalidOperationException("The application cleanup owner is closing.");
        updateDepth++;
        try { action(); }
        finally { updateDepth--; RequestCleanup(); }
    }

    internal void RequestCleanup()
    {
        dispatcher.VerifyAccess();
        if (closed) return;
        requested = true;
        if (updateDepth != 0 || draining || pending is not null || closing) return;
        pending = dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(Drain));
        cleanupOperation = pending;
    }

    internal void CloseBeforeDispatcherShutdown()
    {
        dispatcher.VerifyAccess();
        if (closed) return;
        if (updateDepth != 0 || draining)
            throw new InvalidOperationException("Close the cleanup owner after updates and callbacks return.");
        closing = true;
        pending?.Abort();
        pending = null;
        Drain();
        if (LastError is not null)
        {
            closing = false;
            throw new InvalidOperationException("Shutdown cleanup failed; the application owner must retry or report it.", LastError);
        }
        closed = true;
        dispatcher.Hooks.OperationCompleted -= OnOperationCompleted;
    }

    private void OnOperationCompleted(object? sender, DispatcherHookEventArgs args)
    {
        // Observe application work without making cleanup schedule itself forever.
        if (args.Operation != cleanupOperation) RequestCleanup();
    }

    private void Drain()
    {
        pending = null;
        if (closed || draining || updateDepth != 0) return;
        requested = false;
        draining = true;
        try { cleanup(); LastError = null; CleanupCount++; }
        catch (Exception error) { LastError = error; FailureCount++; reportFailure?.Invoke(error); }
        finally { draining = false; }
        if (requested && !closing) RequestCleanup();
    }
}

