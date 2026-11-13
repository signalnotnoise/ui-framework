using System.Runtime.InteropServices;

namespace WebViewAcceptance;

// Win32 INPUT/KEYBDINPUT layout for the x64 acceptance runner.
[StructLayout(LayoutKind.Explicit, Size = 40)]
internal struct KeyboardPacket
{
    [FieldOffset(0)] internal uint Type;
    [FieldOffset(8)] internal ushort Key;
    [FieldOffset(10)] internal ushort Scan;
    [FieldOffset(12)] internal uint Flags;
    [FieldOffset(16)] internal uint Time;
    [FieldOffset(24)] internal nint ExtraInfo;
}
