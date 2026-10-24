using System.Text.Json;
using Microsoft.Diagnostics.Tracing;

internal static class Program
{
    private static int Main(string[] args)
    {
        var output = Path.GetFullPath(args[1]);
        if (File.Exists(output)) throw new InvalidOperationException("Use a new output path.");
        var events = new List<object>();
        using var source = new EventPipeEventSource(args[0]);
        void Record(TraceEvent item)
        {
            var payload = new Dictionary<string, string?>();
            for (var index = 0; index < item.PayloadNames.Length; index++)
                payload[item.PayloadNames[index]] = item.PayloadValue(index)?.ToString();
            events.Add(new { item.ProviderName, item.EventName, item.ThreadID,
                Milliseconds = item.TimeStampRelativeMSec, Payload = payload });
        }
        source.Dynamic.All += item => { if (item.ProviderName == "SignalNotNoise-Comparison") Record(item); };
        source.Clr.All += item =>
        {
            if (item.EventName.StartsWith("GC/") || item.EventName.StartsWith("Contention/")) Record(item);
        };
        source.Process();
        File.WriteAllText(output, JsonSerializer.Serialize(events, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Extracted {events.Count} events");
        return 0;
    }
}
