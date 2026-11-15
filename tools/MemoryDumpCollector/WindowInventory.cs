using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

// Read only window class/ownership metadata for the explicitly selected test process.
internal static class WindowInventory
{
    private delegate bool Visitor(nint window, nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint thread, Visitor visitor, nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumChildWindows(nint parent, Visitor visitor, nint parameter);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint pid);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(nint window, StringBuilder name, int maximum);
    [DllImport("user32.dll")] private static extern uint GetGuiResources(nint process, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern nint FindWindowEx(nint parent, nint after, string? className, string? title);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern nint GetWindowLongPtr(nint window, int index);

    internal static void Write(Process process, string path)
    {
        var modules = process.Modules.Cast<ProcessModule>().Select(module => new { Base = module.BaseAddress, module.ModuleName }).ToArray();
        var windows = new List<object>();
        var seen = new HashSet<nint>();
        bool Add(nint window, nint parent)
        {
            var thread = GetWindowThreadProcessId(window, out var pid);
            if (pid != process.Id || !seen.Add(window)) return true;
            var name = new StringBuilder(1024);
            var length = GetClassName(window, name, name.Capacity);
            var instance = GetWindowLongPtr(window, -6);
            var module = modules.FirstOrDefault(module => module.Base == instance)?.ModuleName;
            windows.Add(new { Module = module, Instance = $"0x{instance:x}", MessageOnly = parent == (nint)(-3), Window = $"0x{window:x}", Parent = $"0x{parent:x}", ThreadId = thread,
                Class = length > 0 ? name.ToString() : null });
            return true;
        }
        foreach (ProcessThread thread in process.Threads)
        {
            using (thread)
                EnumThreadWindows((uint)thread.Id, (window, _) =>
                { Add(window, 0); EnumChildWindows(window, (child, _) => Add(child, window), 0); return true; }, 0);
        }
        nint message = 0;
        while ((message = FindWindowEx((nint)(-3), message, null, null)) != 0) Add(message, (nint)(-3));
        using var file = new FileStream(path, FileMode.CreateNew);
        JsonSerializer.Serialize(file, new { ProcessId = process.Id, GdiObjects = GetGuiResources(process.Handle, 0),
            UserObjects = GetGuiResources(process.Handle, 1), Windows = windows,
            Limitation = "Window classes only; not all USER/GDI resources are windows. Enumeration and dump are not atomic. Includes visible/hidden thread windows and message-only windows; module is GWLP_HINSTANCE matched to a loaded module, not an allocation stack." },
            new JsonSerializerOptions { WriteIndented = true });
    }
}
