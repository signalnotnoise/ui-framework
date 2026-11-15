using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace MeasurementTools;

// Diagnostic-only: release reporting roots after all workload measurements have finished.
internal static class ReportingRetention
{
    internal static (JsonElement Samples, object Measurement, int Survivors) Release(List<object> samples, string path)
    {
        using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            JsonSerializer.Serialize(file, samples);
        var count = samples.Count;
        var capacity = samples.Capacity;
        var references = Track(samples);
        var before = CollectedBytes();
        samples.Clear();
        samples.TrimExcess();
        var after = CollectedBytes();
        var survivors = references.Count(reference => reference.IsAlive);
        // Reload only AFTER the measurement, preserving the original report schema/evidence.
        using var input = File.OpenRead(path);
        var restored = JsonSerializer.Deserialize<JsonElement>(input);
        if (restored.GetArrayLength() != count) throw new InvalidOperationException("Reporting evidence lost samples.");
        return (restored, new { Protocol = "release-reporting-roots-v1", Samples = count,
            PreviousListCapacity = capacity, BeforeCollectedManagedBytes = before,
            AfterCollectedManagedBytes = after, ObservedReleasedBytes = before - after,
            SampleSurvivors = survivors, MeasurementBeforeReportReload = true,
            Scope = "End-of-run managed reporting roots only; signed GC delta is not a corrected trend, process-memory budget or runtime speedup." }, survivors);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference[] Track(List<object> samples) => samples.Select(sample => new WeakReference(sample)).ToArray();

    private static long CollectedBytes()
    {
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        return GC.GetTotalMemory(false);
    }
}
