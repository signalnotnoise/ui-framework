using System.Windows;
using UI_Framework.Wpf;

namespace LogViewer;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        using var cleanup = new WpfComCleanupPolicy(app.Dispatcher, error => Console.Error.WriteLine(error));
        try
        {
            if (args.Length > 0 && args[0] == "--verify")
            {
                LogViewerChecks.Run(args.Length > 1 ? args[1] : "artifacts/log-viewer");
                return 0;
            }
            var window = new LogWindow();
            window.Closed += (_, _) => { cleanup.Dispose(); app.Shutdown(); };
            return app.Run(window);
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
