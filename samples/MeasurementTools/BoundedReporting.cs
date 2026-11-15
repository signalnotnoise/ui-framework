using System.IO;
using System.Text.Json;

namespace MeasurementTools;

// Keep only a single weak probe between samples; reload the full report after measurement.
internal sealed class BoundedReporting
{
    private readonly string directory;
    private WeakReference? previous;
    private int count, verified;
    private bool completed;

    internal BoundedReporting(string directory)
    {
        this.directory = Path.GetFullPath(directory);
        if (Directory.Exists(this.directory)) throw new IOException("Use a fresh reporting directory.");
        Directory.CreateDirectory(this.directory);
    }

    internal void VerifyPreviousCollected()
    {
        if (previous is null) return;
        if (previous.IsAlive) throw new InvalidOperationException($"Reporting sample {count - 1} remains rooted.");
        previous = null;
        verified++;
    }

    internal void Add(object sample)
    {
        if (completed || previous is not null)
            throw new InvalidOperationException("Verify the previous reporting sample before writing another.");
        using (var stream = new FileStream(Path.Combine(directory, $"{count:D6}.json"),
            FileMode.CreateNew, FileAccess.Write, FileShare.None))
            JsonSerializer.Serialize(stream, sample);
        previous = new WeakReference(sample);
        count++;
    }

    internal (JsonElement Samples, object Measurement) Complete()
    {
        if (completed) throw new InvalidOperationException("Reporting already completed.");
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        VerifyPreviousCollected();
        completed = true;
        // Unbounded report reconstruction is deliberately outside all measured workload batches.
        var restored = new JsonElement[count];
        for (var index = 0; index < count; index++)
        {
            using var stream = File.OpenRead(Path.Combine(directory, $"{index:D6}.json"));
            restored[index] = JsonSerializer.Deserialize<JsonElement>(stream);
        }
        return (JsonSerializer.SerializeToElement(restored), new { Protocol = "bounded-sample-files-v1",
            Samples = count, VerifiedCollectedSamples = verified, MaximumPendingWeakProbes = 1,
            SampleSurvivors = 0, MeasurementBeforeReportReload = true,
            Scope = "No historical strong sample roots during measurement; one weak probe and one sample serialization at a time. Full report reload occurs after measurement; not a whole-process budget." });
    }
}
