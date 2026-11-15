using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

internal static class FileHandleProbe
{
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DuplicateHandle(SafeProcessHandle source, nint handle, nint target,
        out SafeFileHandle duplicate, uint access, [MarshalAs(UnmanagedType.Bool)] bool inherit, uint options);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint GetFileType(SafeFileHandle file);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetFinalPathNameByHandle(SafeFileHandle file, StringBuilder path, uint length, uint flags);

    internal static object Read(Process process, ulong handle)
    {
        // Query only. Never use DUPLICATE_CLOSE_SOURCE or read/write the endpoint.
        if (!DuplicateHandle(process.SafeHandle, (nint)handle, new nint(-1), out var file, 0, false, 2))
            return new { Handle = $"0x{handle:x}", Error = Marshal.GetLastWin32Error() };
        using (file)
        {
            Console.WriteLine($"Inspecting file handle 0x{handle:x}");
            var kind = GetFileType(file); var typeError = kind == 0 ? Marshal.GetLastWin32Error() : 0;
            // Pipe queries can block behind synchronous I/O. Never query pipe state or data.
            var path = new StringBuilder(32768);
            var size = kind == 1 ? GetFinalPathNameByHandle(file, path, (uint)path.Capacity, 0) : 0;
            var pathError = kind == 1 && size == 0 ? Marshal.GetLastWin32Error() : 0;
            return new { Handle = $"0x{handle:x}", FileType = kind, TypeError = typeError,
                Path = size > 0 && size < path.Capacity ? path.ToString() : null, PathError = pathError };
        }
    }
}
