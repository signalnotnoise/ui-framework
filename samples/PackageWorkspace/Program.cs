using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

namespace PackageWorkspace;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--check")
        {
            WorkspaceAcceptance.Run(args[1], !args.Contains("--full-list"));
            return;
        }
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        using var cleanup = new WpfComCleanupPolicy(app.Dispatcher, e => Console.Error.WriteLine(e));
        var model = new WorkspaceModel();
        using var host = CreateHost(model);
        var window = new Window { Title = "Package workspace", Width = 1040, Height = 760, MinWidth = 760, MinHeight = 650, Content = host };
        window.Closed += (_, _) => { window.Content = null; host.Dispose(); cleanup.Dispose(); app.Shutdown(); };
        app.Run(window);
    }

    internal static ViewHost CreateHost(WorkspaceModel model)
    {
        var host = new ViewHost(() => Navigation(model.History, route => route == "workspace"
            ? Component<WorkspaceScreen>(screen => screen.Model = model)
            : VStack(Text("Release review").FontSize(28), Text(model.Title.Value), Text(model.Notes.Value),
                Button("Back", () => model.History.Back())).Spacing(16).Padding(32), NavigationTransition.None));
        host.FontFamily = new FontFamily("Segoe UI"); host.FontSize = 14;
        ThemeStyles.Apply(host, new ThemeTokens());
        return host;
    }
}
