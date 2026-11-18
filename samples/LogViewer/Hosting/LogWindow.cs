using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using UI_Framework.Wpf;
using static UI_Framework.UI;

namespace LogViewer;

public sealed class LogWindow : Window
{
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(1.5) };
    public LogWorkspace Model { get; } = new();
    public ViewHost Host { get; }

    public LogWindow(bool checking = false)
    {
        Title = "Trace — UI Framework log viewer";
        Width = 1440; Height = 900; MinWidth = 1040; MinHeight = 700;
        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(StudioTheme.Tokens.Canvas));
        FontFamily = new FontFamily("Segoe UI");
        if (checking) { Left = -10000; Top = -10000; ShowInTaskbar = false; ShowActivated = false; }
        new WindowInteropHelper(this).EnsureHandle();
        Host = new ViewHost(() => Component<LogScreen>(screen => screen.Model = Model));
        ApplyTheme(Host);
        Host.SizeChanged += OnViewportChanged;
        Content = Host;
        Model.CopyText = text => {
            try { Clipboard.SetText(text); Model.Feedback.Value = "Selected payload copied to clipboard"; }
            catch (ExternalException) { Model.Feedback.Value = "Clipboard is busy. Try Copy JSON again."; }
        };
        timer.Tick += OnTick;
        Model.Streaming.Changed += OnStreamingChanged;
        Closed += OnClosed;
    }

    private void OnViewportChanged(object sender, SizeChangedEventArgs args)
    {
        Model.ViewportHeight.Value = Math.Max(540, args.NewSize.Height);
        Model.ViewportWidth.Value = Math.Max(900, args.NewSize.Width);
    }
    internal static void ApplyTheme(FrameworkElement root)
    {
        ThemeStyles.Apply(root, StudioTheme.Tokens);
        root.Resources.MergedDictionaries.Add(new ResourceDictionary {
            Source = new Uri("/LogViewer;component/Hosting/ScrollChrome.xaml", UriKind.Relative)
        });
    }
    private void OnTick(object? sender, EventArgs args)
    {
        if (Model.Streaming.Value) Model.Append(3);
    }
    private void OnStreamingChanged()
    {
        if (Model.Streaming.Value) timer.Start();
        else timer.Stop();
    }
    private void OnClosed(object? sender, EventArgs args)
    {
        timer.Stop(); timer.Tick -= OnTick;
        Model.Streaming.Changed -= OnStreamingChanged;
        Host.SizeChanged -= OnViewportChanged;
        Model.CopyText = _ => { };
        Content = null;
        Host.Dispose();
        Closed -= OnClosed;
    }
}
