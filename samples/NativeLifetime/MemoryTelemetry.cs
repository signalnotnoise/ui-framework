using System.Diagnostics;
using System.Runtime.InteropServices;

namespace NativeLifetime;

internal static class MemoryTelemetry
{
    [DllImport("user32.dll")] private static extern uint GetGuiResources(nint process, uint flags);
    internal static object ProcessSnapshot()
    {
        using var process = Process.GetCurrentProcess();
        process.Refresh();
        var gc = GC.GetGCMemoryInfo();
        return new { ManagedBytes = GC.GetTotalMemory(false), gc.HeapSizeBytes, gc.FragmentedBytes,
            gc.TotalCommittedBytes, process.PrivateMemorySize64, process.WorkingSet64, process.HandleCount,
            GdiObjects = GetGuiResources(process.Handle, 0), UserObjects = GetGuiResources(process.Handle, 1) };
    }
    internal static object Read(int batch, bool warmup, long naturalHeap, double milliseconds, long allocated,
        LifetimeCounters counters, Dictionary<string, int> alive)
    {
        using var process = Process.GetCurrentProcess();
        process.Refresh();
        var gc = GC.GetGCMemoryInfo();
        return new { Batch = batch, Warmup = warmup, NaturalManagedBytes = naturalHeap,
            CollectedManagedBytes = GC.GetTotalMemory(false), gc.HeapSizeBytes, gc.FragmentedBytes,
            gc.TotalCommittedBytes, process.PrivateMemorySize64, process.WorkingSet64,
            process.HandleCount, GdiObjects = GetGuiResources(process.Handle, 0), UserObjects = GetGuiResources(process.Handle, 1),
            BatchMilliseconds = milliseconds, UiAllocatedBytes = allocated, Alive = alive,
            counters.Created, counters.Released, counters.BufferCreates, counters.BufferReleases,
            counters.NativeBytes, counters.WrongThreadReleases };
    }
}
