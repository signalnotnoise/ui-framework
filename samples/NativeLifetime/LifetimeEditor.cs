using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace NativeLifetime;

internal sealed class LifetimeEditor : TextBox, IDisposable
{
    private readonly LifetimeCounters counters;
    private readonly CancellationTokenSource cancellation = new();
    private readonly byte[] payload = new byte[16384];
    private NativeBuffer? buffer;
    private bool disposed;
    internal Popup Popup { get; }
    internal Task Completion { get; }
    internal CancellationToken Token { get; }
    internal bool Ready { get; private set; }
    internal Action<string>? Edited { get; set; }

    internal LifetimeEditor(LifetimeCounters counters, Func<CancellationToken, Task<NativeBuffer>>? initialize = null)
    {
        this.counters = counters;
        counters.Created++;
        counters.Track("nativeControl", this); counters.Track("nativePayload", payload);
        counters.Publisher.Changed += OnPublisher;
        TextChanged += OnText;
        Token = cancellation.Token;
        Popup = new Popup { PlacementTarget = this, Placement = PlacementMode.Bottom,
            StaysOpen = true, Child = new TextBlock { Text = "Owned child popup", Padding = new Thickness(12) } };
        counters.Track("popup", Popup);
        Completion = Initialize(initialize ?? (_ => Task.FromResult(new NativeBuffer(counters))));
    }

    private void OnPublisher() => Tag = counters.Publisher.Value;
    private void OnText(object sender, TextChangedEventArgs args) => Edited?.Invoke(Text);

    private async Task Initialize(Func<CancellationToken, Task<NativeBuffer>> initialize)
    {
        try
        {
            var acquired = await initialize(Token).ConfigureAwait(false);
            await Dispatcher.InvokeAsync(() =>
            {
                if (disposed) { acquired.Dispose(); return; }
                buffer = acquired; Ready = true;
            });
        }
        catch (OperationCanceledException) when (Token.IsCancellationRequested)
        {
            // Expected owner-requested cancellation; no resource was acquired.
        }
        catch (Exception error)
        {
            await Dispatcher.InvokeAsync(() => counters.Errors.Add(error.Message));
        }
    }

    public void Dispose()
    {
        Dispatcher.VerifyAccess();
        if (disposed) return;
        disposed = true;
        counters.Released++;
        List<Exception> errors = [];
        void Cleanup(Action action) { try { action(); } catch (Exception error) { errors.Add(error); } }
        Cleanup(() => { Popup.IsOpen = false; Popup.Child = null; Popup.PlacementTarget = null; });
        counters.Publisher.Changed -= OnPublisher;
        TextChanged -= OnText; Edited = null;
        Cleanup(cancellation.Cancel);
        Cleanup(cancellation.Dispose);
        Cleanup(() => buffer?.Dispose()); buffer = null;
        if (errors.Count != 0) throw new AggregateException("Native adapter cleanup failed.", errors);
    }
}
