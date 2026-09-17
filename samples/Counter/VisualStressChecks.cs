using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using UI_Framework.Wpf;
using static UI_Framework.UI;

namespace StressLab;

internal static class VisualStressChecks
{
    public static int Run()
    {
        var checks = 0;
        void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            checks++;
        }
        void Flush() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        using var model = new WorkspaceModel(12) { TelemetryEnabled = false };
        using var host = new ViewHost(() => Component<Dashboard>(screen => screen.Model = model));
        Flush();
        var button = Descendants<Button>(host).Single(control => Equals(control.Content, "▶ Visual stress"));
        button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
        Flush();
        Check(model.VisualStress.Value.Running && model.VisualStress.Value.Step >= 1, "Native button must start visible stress immediately.");
        Check(model.Selected.Value!.Title.Value.StartsWith("Live edit"), "First step must visibly edit the selected row.");
        Check(model.Items.Any(item => item.StressExpanded.Value), "Stress focus must open a row checklist.");
        host.Measure(new Size(1320, 1220));
        host.Arrange(new Rect(0, 0, 1320, 1220));
        host.UpdateLayout();
        var bitmap = new RenderTargetBitmap(1320, 1220, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(host);
        var png = new PngBitmapEncoder();
        png.Frames.Add(BitmapFrame.Create(bitmap));
        Directory.CreateDirectory("artifacts/visual-stress");
        using (var stream = File.Create("artifacts/visual-stress/running.png")) png.Save(stream);
        var beforeTimer = model.StepsCompleted;
        Pump(TimeSpan.FromMilliseconds(800));
        Check(model.StepsCompleted > beforeTimer, "Dispatcher timer must advance the visible sequence.");
        var stop = Descendants<Button>(host).Single(control => Equals(control.Content, "Stop"));
        stop.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
        var stopped = model.StepsCompleted;
        Pump(TimeSpan.FromMilliseconds(450));
        Check(!model.VisualStress.Value.Running && model.StepsCompleted == stopped, "Stop must cancel subsequent timer steps.");
        Check(model.Items.All(item => !item.StressExpanded.Value), "Stop must release temporary row expansion.");

        var seen = new HashSet<int>();
        model.VisualStress.Changed += () => seen.Add(model.VisualStress.Value.Step);
        model.StartVisualStress();
        while (model.VisualStress.Value.Running) { model.AdvanceRun(); Flush(); }
        Check(model.StepsCompleted == 60 && seen.IsSupersetOf(Enumerable.Range(1, 60)), "Restart must execute all 60 steps and finish once.");
        Check(model.VisualStress.Value.Action.StartsWith("Complete") && model.Items.All(item => !item.StressExpanded.Value), "Completion must update the banner and release temporary focus.");
        Check(!model.OnlyOpen.Value && model.Items.Count == 12, "Completion must show the full board without losing items.");
        model.AdvanceRun();
        Check(model.StepsCompleted == 60, "Completed runs must ignore further ticks.");
        model.StartVisualStress();
        model.Start();
        Check(!model.VisualStress.Value.Running && model.StepsCompleted == 0, "Starting the mixed campaign must replace visual stress.");
        model.Stop();
        model.StartVisualStress();
        model.Load(0);
        Check(!model.VisualStress.Value.Running && model.Items.Count == 0, "Loading data must cancel visual stress.");
        model.StartVisualStress();
        Check(model.Items.Count == 1 && model.VisualStress.Value.Running, "Visual stress must handle an empty board.");
        model.Dispose();
        var disposedStep = model.StepsCompleted;
        Pump(TimeSpan.FromMilliseconds(450));
        Check(model.StepsCompleted == disposedStep && !model.VisualStress.Value.Running, "Disposal must stop the timer.");
        host.Dispose();
        Check(model.Counters.Active == 0, "Host disposal must balance component lifetimes.");
        Console.WriteLine($"Passed {checks} visual stress checks.");
        return 0;
    }

    private static void Pump(TimeSpan duration)
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = duration };
        timer.Tick += (_, _) => { timer.Stop(); frame.Continue = false; };
        timer.Start();
        Dispatcher.PushFrame(frame);
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T match) yield return match;
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
            foreach (var descendant in Descendants<T>(child)) yield return descendant;
    }
}
