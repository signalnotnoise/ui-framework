using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FocusComparison;

internal static class ResourceSnapshot
{
    [DllImport("user32.dll")] private static extern uint GetGuiResources(nint process, uint flags);
    internal static object Read()
    {
        using var process = Process.GetCurrentProcess();
        process.Refresh();
        return new { Gdi = GetGuiResources(process.Handle, 0), User = GetGuiResources(process.Handle, 1),
            Handles = process.HandleCount, Heap = GC.GetTotalMemory(false), PrivateBytes = process.PrivateMemorySize64 };
    }
}
