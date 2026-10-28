using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
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
        var smoke = args.Contains("--smoke");
        var path = Path.GetFullPath($"artifacts/fw006/manual-accessibility-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.jsonl");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var log = new StreamWriter(new FileStream(path, FileMode.CreateNew, FileAccess.Write)) { AutoFlush = true };
        void Record(string kind, object? detail = null) => log.WriteLine(JsonSerializer.Serialize(new { utc = DateTime.UtcNow, kind, detail }));
        var app = new Application();
        using var cleanup = new WpfComCleanupPolicy(app.Dispatcher);
        var window = new Window { Title = "FW-006 accessibility acceptance", Width = 1000, Height = 850, FontSize = 14 };
        new WindowInteropHelper(window).EnsureHandle();
        var title = new State<string>("Workspace title");
        var summary = new State<string>("Read-only summary");
        var remember = new State<bool>(false);
        var selected = new State<int>(0);
        var result = new State<string>("No action yet");
        var rows = Enumerable.Range(0, 1000).Select(i => new State<string>($"Record {i} text")).ToArray();
        using var host = new ViewHost(() => VStack(
            TextField(title).AccessibilityLabel("Workspace title"),
            TextField(summary).IsReadOnly(true).AccessibilityLabel("Read-only summary"),
            Picker(["Inbox", "Archive"], selected).AccessibilityLabel("Destination"),
            Toggle("Remember destination", remember),
            HStack(Button("Save", () => result.Value = $"Saved {title.Value}").AccessibilityLabel("Save workspace"),
                Button("Unavailable", () => { }).IsEnabled(false)),
            Text(result.Value),
            VirtualList(rows.Select((state, i) => HStack(
                TextField(state).AccessibilityLabel($"Text for record {i}").Width(360),
                Button("Open", () => result.Value = $"Opened record {i}").AccessibilityLabel($"Open record {i}"))
                .Id($"record-{i}").AccessibilityLabel($"Record {i}")), 300).AccessibilityLabel("Records")
        ).Spacing(8));
        var dark = false;
        void ApplyTheme()
        {
            var theme = dark ? new ThemeTokens { Surface = "#202020", Ink = "#FFFFFF", Border = "#808080", Focus = "#FFFF00" } : new ThemeTokens();
            ThemeStyles.Apply(host, theme);
            window.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.Surface));
            host.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.Ink));
            Record("theme", new { dark, systemHighContrast = SystemParameters.HighContrast });
        }
        var root = new DockPanel { Margin = new Thickness(16) };
        var instructions = new TextBlock { Text = "Use Narrator and keyboard navigation. F6 switches light/dark without moving focus.\nRecord OS, Narrator version, contrast setting, monitor scale and observed behavior below. Smoke logs are not manual acceptance.\n" + path, TextWrapping = TextWrapping.Wrap };
        var notes = new TextBox { AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Height = 70, Margin = new Thickness(0, 8, 0, 8) };
        AutomationProperties.SetName(notes, "Manual acceptance observations and environment");
        var save = new Button { Content = "Record observation", Padding = new Thickness(8), HorizontalAlignment = HorizontalAlignment.Left };
        save.Click += (_, _) => { Record("operator-observation", new { notes = notes.Text, dpi = VisualTreeHelper.GetDpi(window).DpiScaleX, systemHighContrast = SystemParameters.HighContrast }); notes.Clear(); };
        var footer = new StackPanel();
        footer.Children.Add(notes);
        footer.Children.Add(save);
        DockPanel.SetDock(instructions, System.Windows.Controls.Dock.Top);
        DockPanel.SetDock(footer, System.Windows.Controls.Dock.Bottom);
        root.Children.Add(instructions);
        root.Children.Add(footer);
        root.Children.Add(host);
        window.Content = root;
        window.PreviewKeyDown += (_, e) =>
        {
            if (e.Key != Key.F6) return;
            dark = !dark;
            ApplyTheme();
            e.Handled = true;
        };
        window.DpiChanged += (_, _) => Record("dpi", VisualTreeHelper.GetDpi(window).DpiScaleX);
        window.Closed += (_, _) => { host.Dispose(); cleanup.Dispose(); Record("closed"); };
        Record("environment", new { smoke, runtime = Environment.Version.ToString(), os = Environment.OSVersion.ToString(),
            language = InputLanguageManager.Current.CurrentInputLanguage.Name, systemHighContrast = SystemParameters.HighContrast,
            coreSha256 = Hash(typeof(View).Assembly.Location), wpfSha256 = Hash(typeof(ViewHost).Assembly.Location) });
        ApplyTheme();
        var exitCode = 0;
        if (smoke) window.ContentRendered += (_, _) => window.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
        {
            try
            {
                InitializePeers(UIElementAutomationPeer.CreatePeerForElement(window)!);
                var list = Find<ItemsControl>(host).Single(control => AutomationProperties.GetName(control) == "Records");
                var peer = UIElementAutomationPeer.CreatePeerForElement(list)!;
                var discovery = (IItemContainerProvider)peer.GetPattern(PatternInterface.ItemContainer)!;
                var target = discovery.FindItemByProperty(null!, AutomationElementIdentifiers.NameProperty.Id, "Record 999");
                if (target is null) throw new InvalidOperationException("Final row was not discoverable.");
                ((IVirtualizedItemProvider)target.GetPatternProvider(VirtualizedItemPatternIdentifiers.Pattern.Id)).Realize();
                window.UpdateLayout();
                var editor = Find<TextBox>(host).Single(input => input.Text == "Record 999 text");
                if (!editor.Focus()) throw new InvalidOperationException("Realized editor could not receive focus.");
                dark = true;
                ApplyTheme();
                window.UpdateLayout();
                if (!ReferenceEquals(editor, Keyboard.FocusedElement)) throw new InvalidOperationException("Theme replacement moved focus.");
                Record("smoke-pass", "Setup, discovery, realization and retained focus only; no Narrator/contrast/scaling acceptance.");
            }
            catch (Exception error) { exitCode = 1; Record("smoke-error", error.ToString()); }
            finally { window.Close(); }
        }));
        Console.WriteLine(path);
        app.Run(window);
        return exitCode;
    }

    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    private static void InitializePeers(AutomationPeer peer)
    {
        foreach (var child in peer.GetChildren() ?? []) InitializePeers(child);
    }

    private static IEnumerable<T> Find<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T match) yield return match;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            foreach (var child in Find<T>(VisualTreeHelper.GetChild(root, i))) yield return child;
    }
}
