using System.Diagnostics;
using System.Windows.Threading;
using UI_Framework;

namespace StressLab;

public sealed record ProjectSettings(string Name, string Owner, Preferences Preferences);
