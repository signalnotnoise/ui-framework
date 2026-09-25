using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

internal sealed class LayoutProbe : Decorator
{
    internal static bool Enabled { get; set; }
    internal long MeasureCalls { get; private set; }
    internal long ArrangeCalls { get; private set; }
    internal long MeasureTicks { get; private set; }
    internal long ArrangeTicks { get; private set; }
    internal long UnboundedMeasures { get; private set; }
    internal void Reset() => MeasureCalls = ArrangeCalls = MeasureTicks = ArrangeTicks = UnboundedMeasures = 0;
    internal object Snapshot() => new { MeasureCalls, ArrangeCalls, UnboundedMeasures,
        MeasureMilliseconds = MeasureTicks * 1000.0 / Stopwatch.Frequency,
        ArrangeMilliseconds = ArrangeTicks * 1000.0 / Stopwatch.Frequency };
    protected override Size MeasureOverride(Size constraint)
    {
        if (!Enabled) return base.MeasureOverride(constraint);
        MeasureCalls++;
        if (double.IsInfinity(constraint.Width) || double.IsInfinity(constraint.Height)) UnboundedMeasures++;
        var start = Stopwatch.GetTimestamp();
        try { return base.MeasureOverride(constraint); }
        finally { MeasureTicks += Stopwatch.GetTimestamp() - start; }
    }
    protected override Size ArrangeOverride(Size size)
    {
        if (!Enabled) return base.ArrangeOverride(size);
        ArrangeCalls++;
        var start = Stopwatch.GetTimestamp();
        try { return base.ArrangeOverride(size); }
        finally { ArrangeTicks += Stopwatch.GetTimestamp() - start; }
    }
}
