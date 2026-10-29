using System.Windows.Controls;

// Application-owned adapter example, not a framework async API. The initializer
// may ignore cancellation; a late resource still has to be released on the UI thread.
internal sealed class AsyncNativeTestControl : TextBox, IDisposable
{
    private readonly CancellationTokenSource cancellation = new();
    private readonly Action<Exception> reportFailure;
    private NativeInitializationLease? lease;
    private bool disposed;
    internal Task Completion { get; }
    internal CancellationToken InitializationToken { get; }
    internal int DisposeCount { get; private set; }

    internal AsyncNativeTestControl(Func<CancellationToken, Task<NativeInitializationLease>> initialize,
        Action<Exception> reportFailure)
    {
        this.reportFailure = reportFailure;
        InitializationToken = cancellation.Token;
        Completion = Initialize(initialize);
    }

    private async Task Initialize(Func<CancellationToken, Task<NativeInitializationLease>> initialize)
    {
        try
        {
            var acquired = await initialize(InitializationToken).ConfigureAwait(false);
            await Dispatcher.InvokeAsync(() =>
            {
                if (disposed) { acquired.Dispose(); return; }
                lease = acquired;
                Text = acquired.Text;
            });
        }
        catch (OperationCanceledException) when (InitializationToken.IsCancellationRequested)
        {
            // Owner-requested cancellation is expected; no resource was acquired.
        }
        catch (Exception error)
        {
            // Observe failures even after removal, without touching detached UI.
            await Dispatcher.InvokeAsync(() => reportFailure(error));
        }
    }

    public void Dispose()
    {
        Dispatcher.VerifyAccess();
        if (disposed) return;
        disposed = true;
        DisposeCount++;
        cancellation.Cancel();
        cancellation.Dispose();
        lease?.Dispose();
        lease = null;
    }
}
