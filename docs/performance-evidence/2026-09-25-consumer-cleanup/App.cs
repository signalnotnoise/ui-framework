using System.Diagnostics;
using System.IO;
using System.Windows;
using Lab_Feedback_WPF.Services;

namespace Lab_Feedback_WPF;

public sealed class App : Application
{
    private ApplicationComCleanupPolicy? cleanupPolicy;
    internal bool ExperimentalCleanupEnabled => cleanupPolicy is not null;
    internal int CleanupFailureCount => cleanupPolicy?.FailureCount ?? 0;

    [STAThread]
    public static void Main(string[] args)
    {
        var application = new App { ShutdownMode = ShutdownMode.OnMainWindowClose };
        application.ConfigureExperimentalCleanup(args);
        application.Run(new MainWindow());
    }

    internal void ConfigureExperimentalCleanup(string[] args)
    {
        Dispatcher.VerifyAccess();
        if (!args.Contains("--experimental-com-cleanup", StringComparer.Ordinal)) return;
        if (cleanupPolicy is not null) throw new InvalidOperationException("Cleanup policy already installed.");
        cleanupPolicy = new ApplicationComCleanupPolicy(Dispatcher, reportFailure: ReportCleanupFailure);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // MainWindow.Closed disposes shell, grading, panel and terminal owners first.
        // The UI thread's CLR setting cannot be restored; this owner ends with the app.
        try { cleanupPolicy?.CloseBeforeDispatcherShutdown(); }
        catch (Exception error) { ReportCleanupFailure(error); e.ApplicationExitCode = 1; }
        if (CleanupFailureCount != 0) e.ApplicationExitCode = 1;
        base.OnExit(e);
    }

    private static void ReportCleanupFailure(Exception error)
    {
        // Never open a modal dialog or throw from a COM cleanup callback.
        var message = $"{DateTimeOffset.UtcNow:O} Experimental COM cleanup failed: {error}";
        Trace.WriteLine(message);
        try
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Lab Feedback WPF", "Diagnostics");
            Directory.CreateDirectory(folder);
            File.AppendAllText(Path.Combine(folder, "com-cleanup.log"), message + Environment.NewLine);
        }
        catch (Exception logError) when (logError is IOException or UnauthorizedAccessException)
        {
            Trace.WriteLine($"Unable to save COM cleanup diagnostics: {logError.Message}");
        }
    }
}
