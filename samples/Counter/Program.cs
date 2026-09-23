using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UI_Framework.Wpf;
using StressLab;
using static UI_Framework.UI;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Contains("--reference"))
        {
            AppContext.SetSwitch("UI_Framework.UnfilteredObservation", true);
            AppContext.SetSwitch("UI_Framework.IgnoreMemo", true);
        }
        var app = new Application();
        var buttonStyle = new Style(typeof(Button));
        buttonStyle.Setters.Add(new Setter(Control.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(203, 213, 225))));
        buttonStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.White));
        buttonStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
        app.Resources.Add(typeof(Button), buttonStyle);
        if (args.Contains("--visual-check")) return VisualStressChecks.Run();
        if (args.Contains("--navigation-check")) return LaunchNavigationChecks.Run(app);
        if (args.Contains("--layout-editors")) return LayoutEditorComparison.Run(app, args);
        if (args.Contains("--compare")) return StressRunner.Compare(args);
        if (args.Contains("--diagnostic")) return StressRunner.Diagnostics(args);
        if (args.Contains("--stress")) return StressRunner.Run(args);
        if (args.Contains("--showcase")) return LaunchpadWindow.Run(app, args);

        using var model = new WorkspaceModel();
        using var host = new ViewHost(() => Component<Dashboard>(screen => screen.Model = model));
        var window = new Window
        {
            Title = "UI Framework — Binding Stress Lab",
            Width = 1360,
            Height = 1020,
            MinWidth = 1000,
            MinHeight = 700,
            FontFamily = new FontFamily("Segoe UI"),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            Content = new ScrollViewer { Content = host, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }
        };
        return app.Run(window);
    }
}
