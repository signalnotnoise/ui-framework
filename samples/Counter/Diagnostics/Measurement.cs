using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using UI_Framework.Wpf;
using static UI_Framework.UI;

namespace StressLab;

internal sealed record Measurement(string Scenario, double Milliseconds, long BodyBuilds, long Mounts, long Unmounts, long AllocatedBytes);
