using System.Runtime.InteropServices;

namespace NativeLifetime;

// A measured, app-owned unmanaged allocation, not a simulated release counter.
internal sealed class NativeBuffer : IDisposable
{
    private readonly LifetimeCounters counters;
    private readonly int thread = Environment.CurrentManagedThreadId;
    private nint pointer;
    internal const int Size = 32768;
    internal int DisposeCount { get; private set; }
    internal NativeBuffer(LifetimeCounters counters)
    {
        this.counters = counters;
        pointer = Marshal.AllocHGlobal(Size);
        for (var offset = 0; offset < Size; offset += 4096) Marshal.WriteByte(pointer, offset, 1);
        counters.BufferCreates++; counters.NativeBytes += Size;
    }
    public void Dispose()
    {
        if (pointer == 0) return;
        DisposeCount++;
        Marshal.FreeHGlobal(pointer); pointer = 0;
        counters.BufferReleases++; counters.NativeBytes -= Size;
        if (Environment.CurrentManagedThreadId != thread)
        {
            counters.WrongThreadReleases++;
            throw new InvalidOperationException("Native buffer released off its owning UI thread.");
        }
    }
}
