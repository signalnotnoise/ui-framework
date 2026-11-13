using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WebViewAcceptance;

internal static class RetentionTelemetry
{
    [DllImport("user32.dll")] private static extern uint GetGuiResources(nint process, uint flags);
    internal static object Read()
    {
        using var process = Process.GetCurrentProcess();
        process.Refresh();
        return new { ManagedBytes = GC.GetTotalMemory(false), process.PrivateMemorySize64,
            process.WorkingSet64, process.HandleCount, GdiObjects = GetGuiResources(process.Handle, 0),
            UserObjects = GetGuiResources(process.Handle, 1) };
    }
}
