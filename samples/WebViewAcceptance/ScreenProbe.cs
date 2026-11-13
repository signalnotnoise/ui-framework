using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WebViewAcceptance;

internal static class ScreenProbe
{
    [DllImport("user32.dll")] private static extern nint GetDC(nint window);
    [DllImport("user32.dll")] private static extern int ReleaseDC(nint window, nint dc);
    [DllImport("gdi32.dll")] private static extern nint CreateCompatibleDC(nint dc);
    [DllImport("gdi32.dll")] private static extern nint CreateCompatibleBitmap(nint dc, int width, int height);
    [DllImport("gdi32.dll")] private static extern nint SelectObject(nint dc, nint item);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(nint item);
    [DllImport("gdi32.dll")] private static extern bool DeleteDC(nint dc);
    [DllImport("gdi32.dll")] private static extern bool BitBlt(nint target, int x, int y, int width, int height, nint source, int sourceX, int sourceY, uint operation);
    [DllImport("gdi32.dll")] private static extern uint GetPixel(nint dc, int x, int y);

    internal static uint Pixel(Window owner, Point screen)
    {
        KeyboardProbe.RequireForeground(owner);
        var dc = GetDC(0);
        try
        {
            var color = GetPixel(dc, (int)Math.Round(screen.X), (int)Math.Round(screen.Y));
            Probe.Require(color != uint.MaxValue, "Screen pixel unavailable.");
            return color;
        }
        finally { ReleaseDC(0, dc); }
    }
    internal static void Capture(Window owner, FrameworkElement root, string path)
    {
        KeyboardProbe.RequireForeground(owner);
        var origin = root.PointToScreen(new Point(0, 0));
        var scale = PresentationSource.FromVisual(root)!.CompositionTarget.TransformToDevice;
        var width = (int)Math.Ceiling(root.ActualWidth * scale.M11);
        var height = (int)Math.Ceiling(root.ActualHeight * scale.M22);
        var screen = GetDC(0);
        var memory = CreateCompatibleDC(screen);
        var bitmap = CreateCompatibleBitmap(screen, width, height);
        var previous = SelectObject(memory, bitmap);
        try
        {
            Probe.Require(BitBlt(memory, 0, 0, width, height, screen, (int)origin.X, (int)origin.Y, 0x40CC0020), "Screen capture failed.");
            var source = Imaging.CreateBitmapSourceFromHBitmap(bitmap, 0, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(source));
            using var file = File.Create(path); encoder.Save(file);
        }
        finally
        {
            SelectObject(memory, previous); DeleteObject(bitmap); DeleteDC(memory); ReleaseDC(0, screen);
        }
    }
}
