using System.Diagnostics.Tracing;

namespace StressLab;

[EventSource(Name = "SignalNotNoise-Comparison")]
internal sealed class ComparisonEvents : EventSource
{
    [Event(1, Level = EventLevel.Informational, Opcode = EventOpcode.Info)]
    public void Boundary(string phase, int step, bool beginning) => WriteEvent(1, phase, step, beginning);
}
