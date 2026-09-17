using System.Diagnostics;
using System.Windows.Threading;
using UI_Framework;

namespace StressLab;

public sealed record VisualStressState(bool Running, int Step, int Total, string Action);
