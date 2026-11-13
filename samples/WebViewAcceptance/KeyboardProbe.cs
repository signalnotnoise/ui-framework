using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace WebViewAcceptance;

internal static class KeyboardProbe
{
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint count, KeyboardPacket[] input, int size);

    internal static bool IsForeground(Window window) => GetForegroundWindow() == new WindowInteropHelper(window).Handle;
    internal static void RequireForeground(Window window)
        => Probe.Require(IsForeground(window),
            "Test window lost foreground focus; refusing to inject input or sample another window.");

    internal static void Press(Window window, ushort key, ushort modifier = 0)
    {
        var packets = new List<KeyboardPacket>();
        if (modifier != 0) packets.Add(Packet(modifier));
        packets.Add(Packet(key)); packets.Add(Packet(key, 2));
        if (modifier != 0) packets.Add(Packet(modifier, 2));
        Send(window, packets.ToArray());
    }
    internal static void Text(Window window, string value)
        => Send(window, value.SelectMany(c => new[] {
            new KeyboardPacket { Type = 1, Scan = c, Flags = 4 },
            new KeyboardPacket { Type = 1, Scan = c, Flags = 6 } }).ToArray());

    private static KeyboardPacket Packet(ushort key, uint flags = 0) => new() { Type = 1, Key = key, Flags = flags };
    private static void Send(Window window, KeyboardPacket[] packets)
    {
        Probe.Require(IntPtr.Size == 8, "Keyboard acceptance requires an x64 process.");
        RequireForeground(window);
        foreach (var modifier in new[] { 0x10, 0x11, 0x12, 0x5B, 0x5C })
            Probe.Require((GetAsyncKeyState(modifier) & 0x8000) == 0, "A physical modifier is down; refusing input injection.");
        Probe.Require(SendInput((uint)packets.Length, packets, Marshal.SizeOf<KeyboardPacket>()) == packets.Length,
            "Windows did not accept every test input packet.");
    }
}
