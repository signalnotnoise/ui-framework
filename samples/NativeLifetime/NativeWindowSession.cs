using System.ComponentModel;
using System.Windows;
using UI_Framework.Wpf;

namespace NativeLifetime;

// App-owned close sequencing. A timeout keeps the window/dispatcher available;
// it is never reported as successful cleanup of an uncooperative provider.
internal sealed class NativeWindowSession
{
    private readonly Window window;
    private readonly ViewHost host;
    private readonly Task completion;
    private readonly TimeSpan timeout;
    private bool allowClose;
    internal Task? CloseCompletion { get; private set; }

    internal NativeWindowSession(Window window, ViewHost host, Task completion, TimeSpan timeout)
    {
        this.window = window; this.host = host; this.completion = completion; this.timeout = timeout;
        window.Content = host;
        window.Closing += OnClosing;
    }
    private void OnClosing(object? sender, CancelEventArgs args)
    {
        if (allowClose) return;
        args.Cancel = true;
        if (CloseCompletion is null || CloseCompletion.IsCompleted) CloseCompletion = CloseAsync();
    }
    private async Task CloseAsync()
    {
        // Leave the native Closing event before making another Close call.
        await System.Windows.Threading.Dispatcher.Yield();
        Exception? cleanupError = null;
        try { host.Dispose(); }
        catch (Exception error) { cleanupError = error; }
        window.Content = null;
        await completion.WaitAsync(timeout);
        allowClose = true;
        window.Closing -= OnClosing;
        window.Close();
        if (cleanupError is not null) throw new AggregateException("Window closed after cleanup reported a failure.", cleanupError);
    }
}
