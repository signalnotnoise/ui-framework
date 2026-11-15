using System.Runtime.CompilerServices;
using System.Text.Json;
using MeasurementTools;

internal static class Program
{
    private static void Main(string[] args)
    {
        var output = Path.GetFullPath(args.Single());
        if (Directory.Exists(output)) throw new IOException("Use a fresh evidence directory.");
        Directory.CreateDirectory(output);
        var control = ReportingRetention.Release([], Path.Combine(output, "empty.json"));
        var samples = CreateSamples();
        var negativeRoot = samples[0];
        var negative = ReportingRetention.Release(samples, Path.Combine(output, "negative.json"));
        var negativeJson = JsonSerializer.SerializeToElement(negative.Measurement);
        if (negativeJson.GetProperty("SampleSurvivors").GetInt32() != 1)
            throw new InvalidOperationException("Reporting probe missed deliberate retained sample.");
        GC.KeepAlive(negativeRoot);
        var released = ReportingRetention.Release(CreateSamples(), Path.Combine(output, "released.json"));
        var releasedJson = JsonSerializer.SerializeToElement(released.Measurement);
        if (releasedJson.GetProperty("SampleSurvivors").GetInt32() != 0
            || released.Samples.GetArrayLength() != 8
            || released.Samples[7].GetProperty("Index").GetInt32() != 7
            || released.Samples[7].GetProperty("Payload").GetBytesFromBase64().Length != 32768)
            throw new InvalidOperationException("Reporting release or evidence roundtrip failed.");
        File.WriteAllText(Path.Combine(output, "checks.json"), JsonSerializer.Serialize(new
        { Passed = true, Empty = control.Measurement, Negative = negative.Measurement, Released = released.Measurement }));
        Console.WriteLine("PASS: empty control, deliberate retained sample detection, release and evidence roundtrip.");
        var bounded = new BoundedReporting(Path.Combine(output, "bounded"));
        for (var index = 0; index < 128; index++)
        {
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            bounded.VerifyPreviousCollected();
            WriteSample(bounded, index);
        }
        var boundedResult = bounded.Complete();
        if (boundedResult.Samples.GetArrayLength() != 128 || Directory.GetFiles(Path.Combine(output, "bounded")).Length != 128)
            throw new InvalidOperationException("Bounded reporter lost samples.");
        for (var index = 0; index < 128; index++)
            if (boundedResult.Samples[index].GetProperty("Index").GetInt32() != index
                || boundedResult.Samples[index].GetProperty("Payload").GetBytesFromBase64().Length != 32768)
                throw new InvalidOperationException("Bounded sample roundtrip failed.");
        var boundedNegative = new BoundedReporting(Path.Combine(output, "bounded-negative"));
        VerifyDeliberateRoot(boundedNegative);
        var negativeResult = boundedNegative.Complete();
        File.WriteAllText(Path.Combine(output, "bounded-checks.json"), JsonSerializer.Serialize(new
        { Passed = true, Bounded = boundedResult.Measurement, NegativeReleased = negativeResult.Measurement }));
        Console.WriteLine("PASS: 128 bounded samples collected between writes, full payload/order preservation, deliberate-root detection and release.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WriteSample(BoundedReporting reporting, int index) =>
        reporting.Add(new { Index = index, Payload = new byte[32768] });

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void VerifyDeliberateRoot(BoundedReporting reporting)
    {
        var root = new { Payload = new byte[32768] };
        reporting.Add(root);
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        var detected = false;
        try { reporting.VerifyPreviousCollected(); }
        catch (InvalidOperationException) { detected = true; }
        if (!detected) throw new InvalidOperationException("Bounded probe missed deliberate retention.");
        GC.KeepAlive(root);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static List<object> CreateSamples() => Enumerable.Range(0, 8)
        .Select(index => (object)new { Index = index, Payload = new byte[32768] }).ToList();
}
