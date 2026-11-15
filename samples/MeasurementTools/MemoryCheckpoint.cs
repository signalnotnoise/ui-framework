using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace MeasurementTools;

// Explicit diagnostic rendezvous; absent from normal acceptance runs unless opted in.
internal static class MemoryCheckpoint
{
    internal static bool Enabled => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("UI_FRAMEWORK_MEMORY_CHECKPOINTS"));

    internal static async Task Pause(string phase)
    {
        var directory = Environment.GetEnvironmentVariable("UI_FRAMEWORK_MEMORY_CHECKPOINTS");
        if (string.IsNullOrEmpty(directory)) return;
        Directory.CreateDirectory(directory);
        var ready = Path.Combine(directory, phase + ".ready.json");
        var resume = Path.Combine(directory, phase + ".resume");
        using (var stream = new FileStream(ready, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            JsonSerializer.Serialize(stream, new { Phase = phase, ProcessId = Environment.ProcessId,
                Protocol = "memory-attribution-paused-v1", ReleaseGate = false });
        var timer = Stopwatch.StartNew();
        while (!File.Exists(resume))
        {
            if (timer.Elapsed > TimeSpan.FromSeconds(90)) throw new TimeoutException($"Diagnostic checkpoint was not resumed: {phase}");
            await Task.Delay(100);
        }
    }
}
