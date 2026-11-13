using System.IO;
using System.Text.Json;
using System.Windows;
using UI_Framework.Wpf;

namespace WebViewAcceptance;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length is not (2 or 3)) throw new ArgumentException("Usage: WebViewAcceptance <mode> <new-output-directory> [--interactive]");
        var mode = args[0];
        if (mode is not ("environment-boundary-close" or "control-close" or "popup-success" or "popup-parent-close" or "popup-child-close" or "focus-keyboard" or "airspace" or "initialization-recovery" or "runtime-missing-recovery" or "navigation-recovery" or "renderer-recovery" or "browser-recovery" or "stalled-success-close" or "stalled-fault-close" or "script-child-close" or "script-parent-close" or "retention-fresh" or "retention-reused" or "pointer-bounds"))
            throw new ArgumentException("Unknown mode.");
        var interactive = mode is "focus-keyboard" or "airspace" or "pointer-bounds";
        if (interactive && (args.Length != 3 || args[2] != "--interactive"))
            throw new ArgumentException("This case activates a desktop window, sends test keystrokes and captures pixels. Explicit --interactive opt-in is required.");
        if (args.Length == 3 && (!interactive || args[2] != "--interactive"))
            throw new ArgumentException("Unexpected interactive argument.");
        var output = Path.GetFullPath(args[1]);
        if (Directory.Exists(output)) throw new IOException("Use a fresh output directory.");
        Directory.CreateDirectory(output);
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        using var cleanup = new WpfComCleanupPolicy(app.Dispatcher);
        object? result = null;
        Exception? failure = null;
        var exit = 1;
        app.Dispatcher.InvokeAsync(async () =>
        {
            try
            {
                result = mode is "focus-keyboard" or "airspace" or "pointer-bounds"
                    ? await InteractionChecks.Run(mode, Path.Combine(output, "isolated-profile"), output)
                    : mode.StartsWith("retention-", StringComparison.Ordinal)
                    ? await RetentionChecks.Run(mode, Path.Combine(output, "isolated-profile"), output)
                    : mode.StartsWith("stalled-", StringComparison.Ordinal) || mode.StartsWith("script-", StringComparison.Ordinal)
                    ? await ShutdownChecks.Run(mode, Path.Combine(output, "isolated-profile"))
                    : mode.EndsWith("-recovery", StringComparison.Ordinal)
                    ? await FailureChecks.Run(mode, Path.Combine(output, "isolated-profile"))
                    : await AcceptanceSuite.Run(mode, Path.Combine(output, "isolated-profile"));
                cleanup.CloseBeforeDispatcherShutdown();
                exit = 0;
            }
            catch (Exception error) { failure = error; Console.Error.WriteLine(error); }
            finally { app.Shutdown(exit); }
        });
        app.Run();
        File.WriteAllText(Path.Combine(output, "report.json"), JsonSerializer.Serialize(new
        {
            Mode = mode, Passed = exit == 0, Result = result, Error = failure?.ToString(),
            DispatcherShutdownFinished = app.Dispatcher.HasShutdownFinished, CleanupPolicyClosed = cleanup.IsClosed,
            DotNet = Environment.Version.ToString(), WebViewSdk = "1.0.4314.35", ReleaseGate = false
        }, new JsonSerializerOptions { WriteIndented = true }));
        return exit;
    }
}
