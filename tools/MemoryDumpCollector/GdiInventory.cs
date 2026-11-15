using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Win32.SafeHandles;

// Diagnostic-only x64 ABI: PHNT PEB.GdiSharedHandleTable and GDI_HANDLE_ENTRY.
// Never dereferences kernel pointers or changes another process's handles.
internal static class GdiInventory
{
    [DllImport("ntdll.dll")] private static extern int NtQueryInformationProcess(SafeProcessHandle process, int info, byte[] buffer, int size, out int returned);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool ReadProcessMemory(SafeProcessHandle process, nint address, byte[] buffer, nuint size, out nuint read);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool IsWow64Process2(SafeProcessHandle process, out ushort machine, out ushort native);
    [DllImport("user32.dll")] private static extern uint GetGuiResources(nint process, uint flags);
    [DllImport("gdi32.dll")] private static extern nint CreateSolidBrush(uint color);
    [DllImport("gdi32.dll")] private static extern nint CreatePen(int style, int width, uint color);
    [DllImport("gdi32.dll")] private static extern nint CreateCompatibleDC(nint dc);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(nint value);
    [DllImport("gdi32.dll")] private static extern bool DeleteDC(nint value);
    [DllImport("gdi32.dll")] private static extern uint GetObjectType(nint value);

    private static byte[] Read(Process process, nint address, int size)
    {
        var bytes = new byte[size];
        if (!ReadProcessMemory(process.SafeHandle, address, bytes, (nuint)size, out var read) || read != (nuint)size)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Incomplete GDI table read.");
        return bytes;
    }

    private static Dictionary<uint, string> Enumerate(Process process)
    {
        if (!Environment.Is64BitProcess || !IsWow64Process2(process.SafeHandle, out var machine, out var native)
            || machine != 0 || native != 0x8664 || process.Id > ushort.MaxValue)
            throw new PlatformNotSupportedException("GDI ABI requires native x64 and a non-truncated 16-bit process ID.");
        var basic = new byte[48];
        var status = NtQueryInformationProcess(process.SafeHandle, 0, basic, basic.Length, out _);
        if (status < 0) throw new InvalidOperationException($"ProcessBasicInformation failed: 0x{status:x8}");
        var peb = (nint)BitConverter.ToInt64(basic, 8);
        var table = (nint)BitConverter.ToInt64(Read(process, peb + 0xf8, 8));
        if (table == 0) throw new InvalidOperationException("Process has no GDI shared table.");
        var bytes = Read(process, table, 0xffff * 24);
        var result = new Dictionary<uint, string>();
        for (var index = 0; index < 0xffff; index++)
        {
            var offset = index * 24;
            if (BitConverter.ToUInt16(bytes, offset + 8) != process.Id) continue;
            var unique = BitConverter.ToUInt16(bytes, offset + 12);
            var type = unique & 0x7f;
            if (bytes[offset + 14] == 0) continue;
            var handle = ((uint)unique << 16) | (uint)index;
            result.Add(handle, type switch
            {
                1 => "DC", 0x21 => "AlternateDC", 4 => "Region", 5 => "Bitmap", 0x25 => "DibSection",
                8 => "Palette", 10 => "Font", 16 => "Brush", 0x30 => "Pen", 0x50 => "ExtendedPen",
                _ => $"UnknownClientType-0x{type:x}"
            });
        }
        return result;
    }

    internal static void Write(Process process, string path)
    {
        var entries = Enumerate(process);
        using var stream = new FileStream(path, FileMode.CreateNew);
        JsonSerializer.Serialize(stream, new { ProcessId = process.Id, Os = Environment.OSVersion.VersionString,
            Abi = "native-x64-peb-f8-entry24-phnt", ReportedGdiObjects = GetGuiResources(process.Handle, 0),
            Enumerated = entries.Count, Objects = entries.Select(pair => new { Handle = $"0x{pair.Key:x8}", Type = pair.Value }),
            Limitation = "Undocumented ABI guarded by explicit controls; point-in-time shared table, non-atomic with counts. No creation stacks, byte sizes or ownership inference from type alone." }, new JsonSerializerOptions { WriteIndented = true });
    }

    internal static void Verify(string path)
    {
        var brush = CreateSolidBrush(0x123456);
        var pen = CreatePen(0, 3, 0x654321);
        var dc = CreateCompatibleDC(0);
        try
        {
            if (brush == 0 || pen == 0 || dc == 0) throw new InvalidOperationException("GDI control creation failed.");
            using var process = Process.GetCurrentProcess();
            var entries = Enumerate(process);
            foreach (var (handle, name, objectType) in new[] { (brush, "Brush", 2u), (pen, "Pen", 1u), (dc, "DC", 10u) })
                if (!entries.TryGetValue((uint)handle, out var found) || found != name || GetObjectType(handle) != objectType)
                    throw new InvalidOperationException($"GDI ABI control failed for {name}.");
            Write(process, path);
            Console.WriteLine("GDI table controls passed: exact owned brush, pen and memory DC handles/types.");
        }
        finally
        {
            var success = (brush == 0 || DeleteObject(brush)) & (pen == 0 || DeleteObject(pen)) & (dc == 0 || DeleteDC(dc));
            if (!success) throw new InvalidOperationException("GDI control cleanup failed.");
        }
    }
}
