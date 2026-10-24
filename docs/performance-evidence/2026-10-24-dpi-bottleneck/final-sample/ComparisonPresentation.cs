using System.Windows.Interop;

namespace StressLab;

// Benchmark-owned WPF initialization; application windows remain app-owned.
internal static class ComparisonPresentation
{
    internal static void Initialize()
    {
        using var source = new HwndSource(new HwndSourceParameters("DPI cache diagnostic")
        {
            Width = 1, Height = 1, PositionX = -10000, PositionY = -10000,
            WindowStyle = unchecked((int)0x88000000)
        });
    }
}
