using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using UI_Framework.Wpf;

namespace NativeLifetime;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        string Value(string name, string fallback) { var index = Array.IndexOf(args, name); return index < 0 ? fallback : args[index + 1]; }
        var mode = Value("--mode", "window");
        if (mode is not ("window" or "memory" or "negative" or "retained")) throw new ArgumentException("Unknown probe mode.");
        int Count(string name, string fallback)
        {
            var value = int.Parse(Value(name, fallback));
            if (value < (name == "--warmups" ? 0 : 1) || value > 10000) throw new ArgumentOutOfRangeException(name);
            return value;
        }
        var path = Path.GetFullPath(Value("--report", "artifacts/native-lifetime.json"));
        if (File.Exists(path)) throw new InvalidOperationException("Use a new report path to preserve evidence.");
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        using var policy = new WpfComCleanupPolicy(app.Dispatcher);
        object? result = null;
        Exception? failure = null;
        var exit = 1;
        app.Dispatcher.InvokeAsync(async () =>
        {
            try
            {
                result = mode == "window" ? await WindowChecks.Run() : mode == "retained"
                    ? await RetentionWorkload.Run(Count("--rows", "500")) : await MemoryWorkload.Run(
                    Count("--batches", "12"), Count("--cycles", "4"),
                    Count("--rows", "500"), Count("--warmups", "3"), mode == "negative");
                policy.CloseBeforeDispatcherShutdown();
                exit = 0;
            }
            catch (Exception error) { failure = error; Console.Error.WriteLine(error); }
            finally { app.Shutdown(exit); }
        });
        app.Run();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(new { Mode = mode, Passed = exit == 0,
            DispatcherShutdownFinished = app.Dispatcher.HasShutdownFinished, CleanupPolicyClosed = policy.IsClosed,
            Runtime = Environment.Version.ToString(), Result = result, Error = failure?.ToString(),
            AfterDispatcherShutdown = MemoryTelemetry.ProcessSnapshot() },
            new JsonSerializerOptions { WriteIndented = true }));
        return exit;
    }
}
