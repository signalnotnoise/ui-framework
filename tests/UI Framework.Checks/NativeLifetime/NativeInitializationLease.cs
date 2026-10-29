internal sealed class NativeInitializationLease(string text) : IDisposable
{
    internal string Text { get; } = text;
    internal int DisposeCount { get; private set; }
    public void Dispose() => DisposeCount++;
}
