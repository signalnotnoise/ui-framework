using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal static class NativeProbe
{
    [DllImport("ComLifetimeProbe", CallingConvention = CallingConvention.Cdecl)]
    private static extern nint CreateProbe();
    [DllImport("ComLifetimeProbe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int ActiveProbes();
    [DllImport("ComLifetimeProbe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int WrongThreadReleases();

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static WeakReference CreateWrapper()
    {
        var pointer = CreateProbe();
        try { return new WeakReference(Marshal.GetObjectForIUnknown(pointer)); }
        finally { Marshal.Release(pointer); }
    }
}
