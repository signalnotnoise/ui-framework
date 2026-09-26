using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace UI_Framework.Wpf;

/// <summary>
/// Owns deferred COM wrapper cleanup for one application STA.
/// Install once before creating WPF controls, close after disposing native hosts,
/// and before shutting down the dispatcher.
/// </summary>
public sealed class WpfComCleanupPolicy : IDisposable
{
    [ThreadStatic] private static bool installed;
    private readonly Dispatcher dispatcher;
    private readonly Action cleanup;
    private readonly Action<Exception>? reportFailure;
    private DispatcherOperation? pending;
    private DispatcherOperation? cleanupOperation;
    private int updateDepth;
    private bool requested;
    private bool draining;
    private bool closing;
    private bool closed;

    public WpfComCleanupPolicy(Dispatcher dispatcher, Action<Exception>? reportFailure = null)
        : this(dispatcher, Marshal.CleanupUnusedObjectsInCurrentContext, reportFailure)
    {
    }

    internal WpfComCleanupPolicy(Dispatcher dispatcher, Action cleanup, Action<Exception>? reportFailure = null)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(cleanup);
        dispatcher.VerifyAccess();
        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA || installed)
            throw new InvalidOperationException("Requires an application-owned STA thread with one cleanup policy.");
        this.dispatcher = dispatcher;
        this.cleanup = cleanup;
        this.reportFailure = reportFailure;
        Thread.CurrentThread.DisableComObjectEagerCleanup();
        installed = true;
        dispatcher.Hooks.OperationCompleted += OnOperationCompleted;
    }

    public int CleanupCount { get; private set; }
    public Exception? LastError { get; private set; }
    public int FailureCount { get; private set; }
    public bool IsClosed => closed;

    public void RunUpdate(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        dispatcher.VerifyAccess();
        if (closing || closed) throw new InvalidOperationException("The application cleanup owner is closing.");
        updateDepth++;
        try { action(); }
        finally
        {
            updateDepth--;
            RequestCleanup();
        }
    }

    public void RequestCleanup()
    {
        dispatcher.VerifyAccess();
        if (closed) return;
        requested = true;
        if (updateDepth != 0 || draining || pending is not null || closing) return;
        pending = dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(Drain));
        cleanupOperation = pending;
    }

    public void CloseBeforeDispatcherShutdown()
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

    public void Dispose() => CloseBeforeDispatcherShutdown();

    private void OnOperationCompleted(object? sender, DispatcherHookEventArgs args)
    {
        if (args.Operation != cleanupOperation) RequestCleanup();
    }

    private void Drain()
    {
        pending = null;
        if (closed || draining || updateDepth != 0) return;
        requested = false;
        draining = true;
        try
        {
            cleanup();
            LastError = null;
            CleanupCount++;
        }
        catch (Exception error)
        {
            LastError = error;
            FailureCount++;
            reportFailure?.Invoke(error);
        }
        finally { draining = false; }
        if (requested && !closing) RequestCleanup();
    }
}
