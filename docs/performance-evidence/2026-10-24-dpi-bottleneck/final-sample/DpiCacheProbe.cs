using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;

namespace StressLab;

internal static class DpiCacheProbe
{
    internal static int Run(string[] args)
    {
        var index = Array.IndexOf(args, "--report");
        var path = Path.GetFullPath(args[index + 1]);
        var type = typeof(FrameworkElement).Assembly.GetType("System.Windows.SystemResources", throwOnError: true)!;
        var getter = type.GetProperty("ProcessDpiAwarenessContextValue", BindingFlags.Static | BindingFlags.NonPublic)!.GetMethod!;
        using var phases = new ComparisonPhaseTrace();
        phases.CaptureDpiCache("cold");
        var before = getter.Invoke(null, null)!;
        phases.Begin("cold-10000-queries", -1);
        for (var query = 0; query < 10000; query++)
            if (!Equals(before, getter.Invoke(null, null))) throw new InvalidOperationException("DPI awareness changed during cold queries.");
        phases.End();
        phases.Begin("empty-presentation-prime", -1);
        var sourcesBefore = PresentationSource.CurrentSources.Cast<PresentationSource>().Count();
        ComparisonPresentation.Initialize();
        phases.End();
        phases.CaptureDpiCache("primed");
        var sourcesAfter = PresentationSource.CurrentSources.Cast<PresentationSource>().Count();
        if (sourcesBefore != sourcesAfter) throw new InvalidOperationException("Priming retained a presentation source.");
        var after = getter.Invoke(null, null)!;
        if (!Equals(before, after)) throw new InvalidOperationException("Priming changed DPI awareness; intervention is not equivalent.");
        phases.Begin("primed-10000-queries", -1);
        for (var query = 0; query < 10000; query++)
            if (!Equals(before, getter.Invoke(null, null))) throw new InvalidOperationException("DPI awareness changed during primed queries.");
        phases.End();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            Kind = "isolated-dpi-cache-diagnostic", QueriesPerMode = 10000,
            BeforeAwareness = before.ToString(), AfterAwareness = after.ToString(),
            SourcesBefore = sourcesBefore, SourcesAfter = sourcesAfter,
            Runtime = Environment.Version.ToString()
        }, new JsonSerializerOptions { WriteIndented = true }));
        phases.Save(path);
        return 0;
    }
}
