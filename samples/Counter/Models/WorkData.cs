using System.Diagnostics;
using System.Windows.Threading;
using UI_Framework;

namespace StressLab;

public sealed record WorkData(string Title, string Owner, bool Done, WorkDetails Details);
