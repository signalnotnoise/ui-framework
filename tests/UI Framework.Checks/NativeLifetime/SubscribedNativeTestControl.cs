using System.Windows.Controls;
using UI_Framework;

internal sealed class SubscribedNativeTestControl : Border, IDisposable
{
    private readonly State<int> publisher;
    private bool disposed;
    internal byte[] Payload { get; } = new byte[32 * 1024];
    internal int Notifications { get; private set; }

    internal SubscribedNativeTestControl(State<int> publisher)
    {
        this.publisher = publisher;
        publisher.Changed += OnChanged;
    }

    private void OnChanged() => Notifications++;

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        publisher.Changed -= OnChanged;
    }
}
