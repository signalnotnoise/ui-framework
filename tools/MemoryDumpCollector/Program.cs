using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static class Program
{
    [DllImport("dbghelp.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool MiniDumpWriteDump(SafeProcessHandle process, uint processId,
        SafeFileHandle file, uint flags, nint exception, nint userStreams, nint callback);

    private static void Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "gdi-check") { GdiInventory.Verify(args[1]); return; }
        if (args.Length == 3 && args[0] == "gui-read")
        {
            using var target = Process.GetProcessById(int.Parse(args[1]));
            GdiInventory.Write(target, args[2] + ".gdi.json");
            WindowInventory.Write(target, args[2] + ".windows.json");
            return;
        }
        if (args.Length == 2 && args[0] == "trace-check")
        {
            HandleTrace.VerifyAbi(args[1]);
            return;
        }
        if (args.Length >= 2 && args[0].StartsWith("trace-", StringComparison.Ordinal))
        {
            using var target = Process.GetProcessById(int.Parse(args[1]));
            if (target.Id == Environment.ProcessId) throw new ArgumentException("Use a separate owned test process.");
            if (args[0] == "trace-enable" && args.Length == 2) HandleTrace.Enable(target);
            else if (args[0] == "trace-read" && args.Length is 4 or 5) HandleTrace.Read(target, args[2], args[3], args.Length == 5 ? args[4] : null);
            else throw new ArgumentException("Usage: trace-enable <pid> or trace-read <pid> <dump> <new-json> [baseline-dump]");
            return;
        }
        if (args.Length != 2) throw new ArgumentException("Usage: MemoryDumpCollector <owned-process-id> <new-dump-path>");
        var pid = int.Parse(args[0]);
        if (pid == Environment.ProcessId) throw new ArgumentException("Capture from a separate process.");
        using var process = Process.GetProcessById(pid);
        using var file = new FileStream(Path.GetFullPath(args[1]), FileMode.CreateNew, FileAccess.Write, FileShare.None);
        // FullMemory | HandleData | ThreadInfo: documented MINIDUMP_TYPE flags.
        if (!MiniDumpWriteDump(process.SafeHandle, (uint)pid, file.SafeFileHandle, 0x1006, 0, 0, 0))
            throw new Win32Exception(Marshal.GetLastWin32Error());
        WindowInventory.Write(process, args[1] + ".windows.json");
        Console.WriteLine($"Captured process {pid} with full memory, handle types and thread information.");
    }
}
