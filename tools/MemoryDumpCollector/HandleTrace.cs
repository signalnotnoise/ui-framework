using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Win32.SafeHandles;

// Process-local diagnostic use of the NT handle tracing ABI (phnt ntpsapi.h).
// Windows-specific, x64-only; never enabled in normal memory measurements.
internal static class HandleTrace
{
    private const int Slots = 0x20000;
    [DllImport("ntdll.dll")]
    private static extern int NtSetInformationProcess(SafeProcessHandle process, int kind, nint data, int length);
    [DllImport("ntdll.dll")]
    private static extern int NtQueryInformationProcess(SafeProcessHandle process, int kind, nint data, int length, out int returned);

    internal static void Enable(Process process)
    {
        if (!Environment.Is64BitProcess) throw new PlatformNotSupportedException("Handle tracing requires x64.");
        var data = Marshal.AllocHGlobal(8);
        try
        {
            Marshal.WriteInt32(data, 0); Marshal.WriteInt32(data, 4, Slots);
            Check(NtSetInformationProcess(process.SafeHandle, 32, data, 8));
        }
        finally { Marshal.FreeHGlobal(data); }
        Console.WriteLine($"Enabled {Slots} handle-history slots for owned diagnostic process {process.Id}.");
    }

    internal static void VerifyAbi(string newFile)
    {
        using var process = Process.GetCurrentProcess();
        Enable(process);
        using var signal = new EventWaitHandle(false, EventResetMode.ManualReset);
        using var file = new FileStream(newFile, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        var fileHandle = file.SafeFileHandle.DangerousGetHandle();
        var eventHandle = signal.SafeWaitHandle.DangerousGetHandle();
        if (JsonSerializer.SerializeToElement(FileHandleProbe.Read(process, (ulong)fileHandle))
                .GetProperty("FileType").GetUInt32() != 1)
            throw new InvalidDataException("Known file was not identified as a disk file.");
        var stacksAvailable = true;
        Verify(fileHandle, 1); Verify(eventHandle, 1);
        file.Dispose(); signal.Dispose();
        Verify(fileHandle, 2); Verify(eventHandle, 2);
        Console.WriteLine($"PASS: known file/event opens and closes match handle, PID and operation. CreationStacksAvailable={stacksAvailable}.");
        if (!stacksAvailable) Console.WriteLine("WARNING: Windows returned empty creation stacks even for known controls. Metadata is usable; creation-stack attribution is unavailable in this session.");

        void Verify(nint handle, int operation)
        {
            const int length = 16 + 160 * 64;
            var data = Marshal.AllocHGlobal(length);
            try
            {
                Marshal.WriteInt64(data, handle); Marshal.WriteInt64(data, 8, 0);
                Check(NtQueryInformationProcess(process.SafeHandle, 32, data, length, out _));
                var count = Marshal.ReadInt32(data, 8);
                if (count < 1 || count > 64) throw new InvalidDataException("Control trace count is invalid.");
                if (Marshal.ReadInt64(data, 16) != handle || Marshal.ReadInt32(data, 40) != operation)
                    throw new InvalidDataException("Control history is not newest-operation first.");
                for (var index = 0; index < count; index++)
                {
                    var entry = data + 16 + 160 * index;
                    if (Marshal.ReadInt64(entry) == handle && Marshal.ReadInt64(entry, 8) == process.Id
                        && Marshal.ReadInt32(entry, 24) == operation)
                    {
                        stacksAvailable &= Enumerable.Range(0, 16).Any(i => Marshal.ReadInt64(entry, 32 + 8 * i) != 0);
                        return;
                    }
                }
                throw new InvalidDataException($"Known handle operation {operation} was not captured.");
            }
            finally { Marshal.FreeHGlobal(data); }
        }
    }

    internal static void Read(Process process, string dumpPath, string output, string? baselinePath)
    {
        var live = ReadHandles(dumpPath);
        var baseline = baselinePath is null ? live : ReadHandles(baselinePath);
        var modules = process.Modules.Cast<ProcessModule>().Select(m => new
        { Name = m.ModuleName, Base = (ulong)m.BaseAddress, Size = m.ModuleMemorySize }).ToArray();
        const int header = 16, stride = 160;
        var length = header + Slots * stride;
        var data = Marshal.AllocHGlobal(length);
        try
        {
            Marshal.WriteInt64(data, 0); Marshal.WriteInt64(data, 8, 0);
            Check(NtQueryInformationProcess(process.SafeHandle, 32, data, length, out _));
            var count = Marshal.ReadInt32(data, 8);
            if (count < 0 || count > Slots) throw new InvalidDataException("Invalid handle trace count.");
            var traces = new List<object>();
            var recordsWithStacks = 0;
            for (var i = 0; i < count; i++)
            {
                var entry = data + header + i * stride;
                var handle = (ulong)Marshal.ReadInt64(entry);
                if (!live.ContainsKey(handle)) continue;
                var frames = new List<object>();
                for (var frame = 0; frame < 16; frame++)
                {
                    var address = (ulong)Marshal.ReadInt64(entry, 32 + frame * 8);
                    if (address == 0) continue;
                    var module = modules.FirstOrDefault(m => address >= m.Base && address - m.Base < (ulong)m.Size);
                    frames.Add(new { Address = $"0x{address:x}", Module = module?.Name,
                        Offset = module is null ? null : $"0x{address - module.Base:x}" });
                }
                traces.Add(new { Index = i, Handle = $"0x{handle:x}", Type = Marshal.ReadInt32(entry, 24),
                    ProcessId = Marshal.ReadInt64(entry, 8), ThreadId = Marshal.ReadInt64(entry, 16), Frames = frames });
                if (frames.Count > 0) recordsWithStacks++;
            }
            using var file = new FileStream(output, FileMode.CreateNew);
            JsonSerializer.Serialize(file, new { Protocol = "nt-handle-history-x64-v1", ReleaseGate = false,
                ProcessId = process.Id, Slots, TotalTraces = count, RingMayHaveWrapped = count == Slots,
                LiveHandles = live.Keys.Select(h => $"0x{h:x}"), Modules = modules, Traces = traces, RecordsWithStacks = recordsWithStacks,
                Files = live.Where(h => h.Value == "File" && (!baseline.TryGetValue(h.Key, out var type) || type != "File"))
                    .Select(h => FileHandleProbe.Read(process, h.Key)).ToArray(),
                Limitation = "History is bounded; missing records are not proof of absence. Handle values can be reused. Capture and query are not atomic." },
                new JsonSerializerOptions { WriteIndented = true });
        }
        finally { Marshal.FreeHGlobal(data); }
    }

    private static Dictionary<ulong, string> ReadHandles(string path)
    {
        using var reader = new BinaryReader(File.OpenRead(path));
        if (reader.ReadUInt32() != 0x504d444d) throw new InvalidDataException("Not a Windows dump.");
        reader.BaseStream.Position = 8;
        var count = reader.ReadUInt32(); var directory = reader.ReadUInt32();
        for (var index = 0; index < count; index++)
        {
            reader.BaseStream.Position = directory + index * 12;
            var kind = reader.ReadUInt32(); var bytes = reader.ReadUInt32(); var offset = reader.ReadUInt32();
            if (kind != 12) continue;
            reader.BaseStream.Position = offset;
            var header = reader.ReadUInt32(); var stride = reader.ReadUInt32(); var handles = reader.ReadUInt32();
            if (header < 16 || stride is not (32 or 40) || (ulong)header + (ulong)stride * handles > bytes)
                throw new InvalidDataException("Invalid handle stream.");
            var result = new Dictionary<ulong, string>();
            for (var item = 0; item < handles; item++)
            {
                reader.BaseStream.Position = offset + header + (long)item * stride;
                var handle = reader.ReadUInt64(); var typeRva = reader.ReadUInt32();
                var type = "<unknown>";
                if (typeRva != 0)
                {
                    reader.BaseStream.Position = typeRva;
                    var size = reader.ReadInt32();
                    if (size < 0 || size > 4096 || size % 2 != 0) throw new InvalidDataException("Invalid handle type name.");
                    var bytesRead = reader.ReadBytes(size);
                    if (bytesRead.Length != size) throw new EndOfStreamException();
                    type = System.Text.Encoding.Unicode.GetString(bytesRead);
                }
                result.Add(handle, type);
            }
            return result;
        }
        throw new InvalidDataException("Dump has no handle data stream.");
    }

    private static void Check(int status)
    {
        if (status < 0) throw new InvalidOperationException($"Handle tracing failed: NTSTATUS 0x{status:x8}.");
    }
}
