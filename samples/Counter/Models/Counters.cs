using System.Diagnostics;
using System.Windows.Threading;
using UI_Framework;

namespace StressLab;

public sealed class Counters
{
    public long Builds, Mounts, Unmounts;
    public long Active => Mounts - Unmounts;
}
