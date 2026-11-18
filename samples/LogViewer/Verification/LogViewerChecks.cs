using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.Windows.Threading;
using UI_Framework.Wpf;
using UI = UI_Framework.UI;

namespace LogViewer;

internal static class LogViewerChecks
{
    public static void Run(string directory)
    {
        var output = Path.GetFullPath(directory);
        if (Directory.Exists(output)) throw new InvalidOperationException("Use a fresh verification directory.");
        Directory.CreateDirectory(output);
        var checks = 0;
        void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); checks++; }
        var window = new LogWindow(checking: true);
        try
        {
            window.Show(); Flush();
            var model = window.Model;
            var host = window.Host;
            Button FindButton(string name) => Find<Button>(host).Single(button => Equals(button.Content, name));
            TextBox Search() => Find<TextBox>(host).Single(box => AutomationProperties.GetName(box) == "Search logs");
            void Click(string name) { FindButton(name).RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent)); Flush(); }
            Check(model.Entries.Count == 2380, "Seed size");
            Check(Find<Button>(host).Count(button => AutomationProperties.GetName(button).StartsWith("Inspect event")) < 40, "Virtualization must bound native rows.");
            var streamScroll = Find<ScrollViewer>(host).Single(scroll => scroll.ExtentHeight > 10000);
            var scrollbar = Find<ScrollBar>(streamScroll).Single(bar => bar.Orientation == Orientation.Vertical);
            var track = (Track)scrollbar.Template.FindName("PART_Track", scrollbar);
            Check(track.ActualWidth <= 10 && track.Thumb.ActualHeight >= 24, "Slim scrollbar retains a grabbable thumb");
            ScrollBar.PageDownCommand.Execute(null, scrollbar); Flush();
            Check(streamScroll.VerticalOffset > 0, "Scrollbar page command still scrolls virtual rows");
            streamScroll.ScrollToTop(); Flush();
            track.Thumb.RaiseEvent(new DragStartedEventArgs(0, 0) { RoutedEvent = Thumb.DragStartedEvent });
            track.Thumb.RaiseEvent(new DragDeltaEventArgs(0, 48) { RoutedEvent = Thumb.DragDeltaEvent });
            track.Thumb.RaiseEvent(new DragCompletedEventArgs(0, 48, false) { RoutedEvent = Thumb.DragCompletedEvent });
            Flush(); Check(streamScroll.VerticalOffset > 0, "Scrollbar thumb still drags virtual rows");
            streamScroll.ScrollToTop(); Flush();
            Capture(host, Path.Combine(output, "wide.png"));
            var payload = Find<TextBox>(host).Single(box => box.IsReadOnly);
            Check(payload.Text.Contains("evt-002380"), "Initial inspector payload");
            var search = Search();
            search.Text = "retry"; Flush();
            Check(model.Visible.Value.All(entry => entry.Payload.Contains("retry", StringComparison.OrdinalIgnoreCase)), "Text filtering");
            Check(ReferenceEquals(search, Search()), "Search control retained while filtering");
            var picker = Find<ComboBox>(host).Single(); picker.SelectedIndex = 1; Flush();
            Check(search.IsTabStop && picker.IsTabStop && FindButton("Clear filters").IsTabStop, "Native filter controls participate in Tab navigation");
            Check(model.Visible.Value.Count > 0 && model.Visible.Value.All(entry => entry.Severity == "Error"), "Severity filter");
            Click("gateway");
            Check(model.Visible.Value.All(entry => entry.Source == "gateway"), "Source filter");
            search.Text = "__no_such_event__"; Flush();
            Check(Find<TextBlock>(host).Any(text => text.Text == "No matching events"), "Empty state");
            Check(ReferenceEquals(payload, Find<TextBox>(host).Single(box => box.IsReadOnly)), "Inspector retained across filters");
            Capture(host, Path.Combine(output, "empty.png"));
            Click("Show all events");
            Check(model.Visible.Value.Count == 2380, "Clear filters restores events");
            Click("Newest first ↓");
            Check(model.Visible.Value[0].Id == 1, "Order toggles by key");
            var rowButton = Find<Button>(host).First(button => AutomationProperties.GetName(button).StartsWith("Inspect event 1:"));
            rowButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent)); Flush();
            Check(model.SelectedId.Value == 1 && payload.Text.Contains("evt-000001"), "Row selection updates retained inspector");
            string? copied = null; model.CopyText = text => copied = text;
            Click("Copy JSON"); Check(copied == model.Selected!.Payload, "Copy action routes selected payload to host");
            Click("Filter this trace"); Check(model.Visible.Value.All(entry => entry.TraceId == model.Selected!.TraceId), "Trace action");
            Click("Clear filters");
            Click("Append 25"); Check(model.Entries.Count == 2405 && model.SelectedId.Value == 1, "Append preserves selection");
            Click("Start stream"); Check(model.Streaming.Value, "Start simulation");
            Pump(TimeSpan.FromSeconds(1.7)); Check(model.Entries.Count > 2405, "Timer ingests simulated events");
            Click("Pause stream"); Check(!model.Streaming.Value, "Pause simulation");
            var count = model.Entries.Count; Pump(TimeSpan.FromSeconds(1.7)); Check(model.Entries.Count == count, "Paused timer does not ingest");
            Click("Hide sources"); Check(model.SourcesHidden.Value, "Collapse sources");
            Click("Show sources"); Check(!model.SourcesHidden.Value, "Restore sources");
            model.StreamWidth.Value = 600; Flush();
            Check(Find<GridSplitter>(host).Count() == 2 && model.StreamWidth.Value == 600, "Two retained split panes");
            var splitter = Find<GridSplitter>(host).OrderBy(control => control.TranslatePoint(new Point(), host).X).Last();
            var presentation = PresentationSource.FromVisual(host)!;
            splitter.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, presentation, 0, Key.Right) { RoutedEvent = Keyboard.KeyDownEvent });
            Flush(); host.UpdateLayout();
            splitter.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, presentation, 0, Key.Right) { RoutedEvent = Keyboard.KeyUpEvent });
            Flush(); Check(model.StreamWidth.Value > 600, "Native keyboard splitter updates bound extent");
            Click("Reset demo"); Check(model.Entries.Count == 2380 && model.SelectedId.Value == 2380, "Reset demo");
            window.Width = 1040; window.Height = 740; Flush();
            Capture(host, Path.Combine(output, "compact.png"));
            foreach (var control in Find<Control>(host).Where(control => control is TextBox or ComboBox or Button && control.IsVisible))
            {
                // Virtual cache rows can legitimately extend outside the scroll viewport.
                if (control is Button button && AutomationProperties.GetName(button).StartsWith("Inspect event")) continue;
                var bounds = control.TransformToAncestor(host).TransformBounds(new Rect(control.RenderSize));
                Check(bounds.Right <= host.ActualWidth + 1 && bounds.Left >= -1, "Compact control horizontal bounds: " + control);
                if (control is Button nativeButton)
                    foreach (var label in Find<TextBlock>(nativeButton))
                        Check(label.DesiredSize.Height <= nativeButton.ActualHeight - nativeButton.Padding.Top - nativeButton.Padding.Bottom,
                            "Button label must fit vertically: " + nativeButton.Content);
            }
            model.Append(6000); Flush();
            Check(model.Entries.Count == LogWorkspace.Capacity && model.Selected is not null, "Bounded ingestion evicts old selection safely");
            Check(model.Entries.Select(entry => entry.Id).Distinct().Count() == LogWorkspace.Capacity, "Unique stable keys");
            var diagnostics = CompareMounts();
            window.Close(); Flush();
            var afterClose = model.Entries.Count; model.Streaming.Value = true;
            Pump(TimeSpan.FromSeconds(1.7)); Check(model.Entries.Count == afterClose, "Window close detaches timer");
            File.WriteAllText(Path.Combine(output, "report.json"), JsonSerializer.Serialize(new { passed = true, checks,
                runtime = Environment.Version.ToString(), captures = new[] { "wide.png", "compact.png", "empty.png" },
                diagnostics, performanceGate = false, physicalKeyboardAndScreenReaderAcceptance = false
            }, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"Passed {checks} log viewer checks. Captures and diagnostics: {output}");
        }
        finally { if (window.IsLoaded) window.Close(); }
    }

    private static object[] CompareMounts()
    {
        var results = new List<object>();
        foreach (var fullList in new[] { true, false, false, true })
        {
            var model = new LogWorkspace(); model.Entries.ReplaceAll(model.Entries.Take(1000).ToArray());
            var before = GC.GetAllocatedBytesForCurrentThread(); var watch = Stopwatch.StartNew();
            using var host = new ViewHost(() => UI.Component<LogStream>(screen => { screen.Model = model; screen.FullList = fullList; }));
            LogWindow.ApplyTheme(host);
            host.Measure(new Size(760, 660)); host.Arrange(new Rect(0, 0, 760, 660)); host.UpdateLayout(); Flush();
            watch.Stop();
            results.Add(new { mode = fullList ? "full-list" : "virtualized", rows = 1000,
                mountMilliseconds = watch.Elapsed.TotalMilliseconds, uiThreadBytes = GC.GetAllocatedBytesForCurrentThread() - before,
                nativeRowButtons = Find<Button>(host).Count(button => AutomationProperties.GetName(button).StartsWith("Inspect event")) });
        }
        return results.ToArray();
    }
    private static void Pump(TimeSpan duration)
    {
        var frame = new DispatcherFrame(); var timer = new DispatcherTimer { Interval = duration };
        timer.Tick += (_, _) => { timer.Stop(); frame.Continue = false; }; timer.Start(); Dispatcher.PushFrame(frame); Flush();
    }
    private static void Flush() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    private static IEnumerable<T> Find<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T match) yield return match;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            foreach (var child in Find<T>(VisualTreeHelper.GetChild(root, i))) yield return child;
    }
    private static void Capture(ViewHost host, string path)
    {
        host.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(host.ActualWidth), (int)Math.Ceiling(host.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(host); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path); encoder.Save(stream);
    }
}
