using System.IO;
using System.Text.Json;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        var logPath = Path.GetFullPath($"artifacts/fw005/manual-ime-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.jsonl");
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
        using var log = new StreamWriter(new FileStream(logPath, FileMode.CreateNew, FileAccess.Write));
        log.AutoFlush = true;
        var app = new Application();
        using var cleanup = new WpfComCleanupPolicy(app.Dispatcher);
        var window = new Window { Title = "FW-005 — physical IME acceptance", Width = 1000, Height = 760, FontSize = 14 };
        new WindowInteropHelper(window).EnsureHandle();
        var root = new DockPanel { Margin = new Thickness(16) };
        var toolbar = new WrapPanel { Margin = new Thickness(0, 12, 0, 12) };
        var status = new TextBlock { Text = "Choose an action, then focus row 0 and start composition within five seconds.", TextWrapping = TextWrapping.Wrap };
        var instructions = new TextBlock { Text = "Edit row 0 with a real IME. Actions run after 5 seconds without another click. Repeat with commit and Escape cancellation. Reset between cases.\nLog: " + logPath, TextWrapping = TextWrapping.Wrap };
        foreach (var element in new FrameworkElement[] { instructions, toolbar, status })
        {
            DockPanel.SetDock(element, System.Windows.Controls.Dock.Top);
            root.Children.Add(element);
        }
        var surface = new ContentControl { Margin = new Thickness(0, 12, 0, 0) };
        root.Children.Add(surface);
        window.Content = root;
        ViewHost? host = null;
        var states = Enumerable.Range(0, 1000).Select(i => new State<string>($"row {i}")).ToArray();
        var outside = new State<string>("Outside editor");
        var ids = new StateList<int>(Enumerable.Range(0, 1000));
        var exitCode = 0;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        Action? pending = null;
        void Record(string kind, object? detail = null) => log.WriteLine(JsonSerializer.Serialize(new { utc = DateTime.UtcNow, kind, detail }));
        var bindings = states.Select((state, id) => new Binding<string>(() => state.Value, text =>
        {
            Record("binding-write", new { id, text });
            state.Value = text;
        })).ToArray();
        void Reset()
        {
            host?.Dispose();
            ids.ReplaceAll(Enumerable.Range(0, 1000));
            for (var id = 0; id < states.Length; id++) states[id].Value = $"row {id}";
            host = new ViewHost(() => VStack(TextField(outside).Id("outside"),
                VirtualList(ids.Select(id => TextField(bindings[id]).AccessibilityLabel($"Row {id}").Height(40).Id(id.ToString())), 400)).Spacing(12));
            surface.Content = host;
            Record("reset");
        }
        void Run(Action action)
        {
            try { action(); status.Text = "Action finished. Check row 0 / next row, focus, candidates and the log. Reset for the next case."; }
            catch (Exception error) { exitCode = 1; Record("error", error.ToString()); status.Text = error.Message; }
        }
        void Add(string label, Action action, bool delayed = true)
        {
            var button = new Button { Content = label, Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 0, 8, 8) };
            button.Click += (_, _) =>
            {
                timer.Stop();
                pending = null;
                if (!delayed) { Run(action); return; }
                status.Text = $"{label} in 5 seconds — focus row 0 and start/update composition now.";
                Record("queued", label);
                pending = () => { Record("action", label); action(); };
                timer.Start();
            };
            toolbar.Children.Add(button);
        }
        void Scroll(bool end)
        {
            var scroll = Find<ScrollViewer>(Find<ItemsControl>(host!).Single()).First();
            if (end) scroll.ScrollToEnd(); else scroll.ScrollToHome();
        }
        Add("Scroll end", () => Scroll(true));
        Add("Scroll home", () => Scroll(false));
        Add("Move row 0 last", () => ids.Move(ids.ToList().IndexOf(0), ids.Count - 1));
        Add("Regenerate", () => Find<ItemsControl>(host!).Single().Items.Refresh());
        Add("Remove row 0", () => ids.Remove(0));
        Add("Dispose host", () => { host?.Dispose(); surface.Content = null; host = null; });
        Add("Reset", Reset, false);
        timer.Tick += (_, _) => { timer.Stop(); var action = pending; pending = null; if (action is not null) Run(action); };
        foreach (var routed in new[] { TextCompositionManager.PreviewTextInputStartEvent,
            TextCompositionManager.PreviewTextInputUpdateEvent, TextCompositionManager.PreviewTextInputEvent })
            root.AddHandler(routed, new TextCompositionEventHandler((_, input) => Record("composition", new
            {
                stage = input.RoutedEvent.Name, text = input.Text, composition = input.TextComposition.CompositionText,
                editor = input.OriginalSource is DependencyObject source ? AutomationProperties.GetName(source) : null
            })), true);
        window.Closing += (_, _) =>
        {
            timer.Stop();
            pending = null;
            try { host?.Dispose(); }
            catch (Exception error) { exitCode = 1; Record("close-error", error.ToString()); }
            finally { cleanup.Dispose(); Record("closed"); }
        };
        Record("environment", new { runtime = Environment.Version.ToString(), os = Environment.OSVersion.ToString(),
            language = InputLanguageManager.Current.CurrentInputLanguage.Name, smoke = args.Contains("--smoke"),
            coreSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(View).Assembly.Location))),
            wpfSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(ViewHost).Assembly.Location))) });
        Reset();
        if (args.Contains("--smoke"))
            window.ContentRendered += (_, _) => app.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
            {
                Run(() =>
                {
                    var input = Find<TextBox>(host!).Single(element => AutomationProperties.GetName(element) == "Row 0");
                    if (!input.Focus()) throw new InvalidOperationException("Acceptance editor could not receive focus.");
                    var list = Find<ItemsControl>(host!).Single();
                    if (Find<TextBox>(list).Count() >= 40 ||
                        list.TransformToAncestor(root).Transform(new Point(0, list.ActualHeight)).Y > root.ActualHeight + 1)
                        throw new InvalidOperationException("Acceptance viewport is unbounded or clipped.");
                    input.Text = "smoke edit";
                    if (states[0].Value != "smoke edit") throw new InvalidOperationException("Acceptance window binding failed.");
                    list.Items.Refresh();
                    host!.UpdateLayout();
                    input.Text = "late detached edit";
                    if (states[0].Value != "smoke edit") throw new InvalidOperationException("Detached acceptance editor wrote to its former binding.");
                    Find<TextBox>(host).First().Focus();
                    Scroll(true);
                    host.UpdateLayout();
                    app.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                    host.UpdateLayout();
                    if (!Find<TextBox>(list).Any(element => AutomationProperties.GetName(element) == "Row 999"))
                        throw new InvalidOperationException("Acceptance scroll action did not reach the final row.");
                    Record("smoke-passed", "Window/binding/regeneration/scroll setup only; physical IME was not tested.");
                });
                window.Close();
            }));
        app.Run(window);
        Console.WriteLine(logPath);
        return exitCode;
    }

    private static IEnumerable<T> Find<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T match) yield return match;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
            foreach (var child in Find<T>(VisualTreeHelper.GetChild(root, index))) yield return child;
    }
}
