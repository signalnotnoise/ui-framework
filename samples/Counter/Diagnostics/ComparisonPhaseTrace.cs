using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Text.Json;
using System.Windows.Interop;
using System.Windows;
using System.Windows.Media;
using System.Security.Cryptography;
using System.Text;
using System.Globalization;

namespace StressLab;

// Diagnostic only. Ordinary release measurements never construct this tracer.
internal sealed class ComparisonPhaseTrace : IDisposable
{
    private readonly ComparisonEvents events = new();
    private readonly List<object> phases = [];
    private readonly uint thread = GetCurrentThreadId();
    private readonly Dictionary<string, object> dpiSnapshots = [];
    private readonly Dictionary<string, object> geometry = [];
    private string phase = "";
    private int step;
    private long started, user, kernel, allocation;
    private int gc0, gc1, gc2;

    internal void Begin(string name, int operation)
    {
        phase = name; step = operation;
        events.Boundary(name, operation, true);
        allocation = GC.GetAllocatedBytesForCurrentThread();
        gc0 = GC.CollectionCount(0); gc1 = GC.CollectionCount(1); gc2 = GC.CollectionCount(2);
        ReadCpu(out kernel, out user);
        started = Stopwatch.GetTimestamp();
    }

    internal void End()
    {
        var ended = Stopwatch.GetTimestamp();
        ReadCpu(out var endKernel, out var endUser);
        var bytes = GC.GetAllocatedBytesForCurrentThread() - allocation;
        phases.Add(new
        {
            Phase = phase, Step = step, StartTimestamp = started, EndTimestamp = ended,
            Milliseconds = Stopwatch.GetElapsedTime(started, ended).TotalMilliseconds,
            UserCpuMilliseconds = (endUser - user) / 10000d,
            KernelCpuMilliseconds = (endKernel - kernel) / 10000d,
            AllocatedBytes = bytes, Gen0 = GC.CollectionCount(0) - gc0,
            Gen1 = GC.CollectionCount(1) - gc1, Gen2 = GC.CollectionCount(2) - gc2
        });
        events.Boundary(phase, step, false);
    }

    internal void Save(string report)
    {
        File.WriteAllText(report + ".phases.json", JsonSerializer.Serialize(new
        {
            Kind = "diagnostic-not-release-gate", UiThreadId = thread,
            EventsEnabled = events.IsEnabled(),
            StopwatchFrequency = Stopwatch.Frequency, Runtime = Environment.Version.ToString(),
            DpiCache = dpiSnapshots, Geometry = geometry, Phases = phases
        }, new JsonSerializerOptions { WriteIndented = true }));
    }

    internal void CaptureDpiCache(string name)
    {
        // Observe the installed runtime for diagnosis; never write private fields.
        dpiSnapshots[name] = new[] { "IsProcessUnaware", "IsProcessSystemAware", "IsProcessPerMonitorDpiAware" }
            .ToDictionary(property => property, property => typeof(HwndTarget)
                .GetProperty(property, BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null));
    }

    internal void CaptureGeometry(FrameworkElement root, string name)
    {
        var shapes = new StringBuilder();
        var counts = new Dictionary<string, int>();
        void Visit(DependencyObject item)
        {
            var type = item.GetType().FullName!;
            counts[type] = counts.GetValueOrDefault(type) + 1;
            shapes.Append(type);
            if (item is FrameworkElement element)
            {
                var offset = VisualTreeHelper.GetOffset(element);
                foreach (var value in new[] { element.ActualWidth, element.ActualHeight, offset.X, offset.Y })
                    shapes.Append('|').Append(value.ToString("F4", CultureInfo.InvariantCulture));
            }
            shapes.AppendLine();
            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(item); index++) Visit(VisualTreeHelper.GetChild(item, index));
        }
        Visit(root);
        var dpi = VisualTreeHelper.GetDpi(root);
        geometry[name] = new { root.ActualWidth, root.ActualHeight, dpi.DpiScaleX, dpi.DpiScaleY,
            Counts = counts, Sha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(shapes.ToString()))) };
    }

    private static void ReadCpu(out long kernelTime, out long userTime)
    {
        if (!GetThreadTimes(GetCurrentThread(), out _, out _, out kernelTime, out userTime))
            throw new Win32Exception(Marshal.GetLastWin32Error());
    }

    public void Dispose() => events.Dispose();

    [DllImport("kernel32.dll", ExactSpelling = true)]
    private static extern nint GetCurrentThread();
    [DllImport("kernel32.dll", ExactSpelling = true)]
    private static extern uint GetCurrentThreadId();
    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetThreadTimes(nint thread, out long creationTime, out long exitTime, out long kernelTime, out long userTime);
}
