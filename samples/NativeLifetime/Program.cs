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
        if (Environment.GetEnvironmentVariable("UI_FRAMEWORK_TRACE_THREAD_EXIT") == "1")
        {
            if (!MeasurementTools.MemoryCheckpoint.Enabled) throw new InvalidOperationException("Thread-exit tracing requires diagnostic checkpoints.");
            var reportIndex = Array.IndexOf(args, "--report");
            if (reportIndex < 0 || reportIndex + 1 >= args.Length) throw new ArgumentException("Thread-exit tracing requires --report.");
            return ThreadExitProbe.Run(() => Run(args), Path.GetFullPath(args[reportIndex + 1]));
        }
        return Run(args);
    }

    private static int Run(string[] args)
    {
        string Value(string name, string fallback) { var index = Array.IndexOf(args, name); return index < 0 ? fallback : args[index + 1]; }
        var mode = Value("--mode", "window");
        if (mode is not ("window" or "memory" or "negative" or "retained" or "automation-retention")) throw new ArgumentException("Unknown probe mode.");
        int Count(string name, string fallback)
        {
            var value = int.Parse(Value(name, fallback));
            if (value < (name == "--warmups" ? 0 : 1) || value > 10000) throw new ArgumentOutOfRangeException(name);
            return value;
        }
        var focusAtBatch = int.Parse(Value("--focus-at-batch", "-1"));
        if (focusAtBatch < -1 || (focusAtBatch >= 0 && mode != "memory")) throw new ArgumentException("Focus requires memory mode and a nonnegative batch index.");
        var automationWarmup = args.Contains("--automation-warmup");
        if (automationWarmup && mode != "memory") throw new ArgumentException("Automation warmup requires memory mode.");
        var path = Path.GetFullPath(Value("--report", "artifacts/native-lifetime.json"));
        if (File.Exists(path)) throw new InvalidOperationException("Use a new report path to preserve evidence.");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        using var policy = new WpfComCleanupPolicy(app.Dispatcher);
        object? result = null;
        Exception? failure = null;
        var exit = 1;
        app.Dispatcher.InvokeAsync(async () =>
        {
            try
            {
                if (automationWarmup) await AutomationRetentionProbe.WarmClient();
                result = mode == "automation-retention" ? await AutomationRetentionProbe.Run()
                    : mode == "window" ? await WindowChecks.Run() : mode == "retained"
                    ? await RetentionWorkload.Run(Count("--rows", "500")) : await MemoryWorkload.Run(
                    Count("--batches", "12"), Count("--cycles", "4"),
                    Count("--rows", "500"), Count("--warmups", "3"), mode == "negative", path, focusAtBatch);
                policy.CloseBeforeDispatcherShutdown();
                exit = 0;
            }
            catch (Exception error) { failure = error; Console.Error.WriteLine(error); }
            finally { app.Shutdown(exit); }
        });
        app.Run();
        if (Environment.GetEnvironmentVariable("UI_FRAMEWORK_FOCUS_TRACE") is "1" or "control")
            Task.Run(() => MeasurementTools.MemoryCheckpoint.Pause("post-shutdown")).GetAwaiter().GetResult();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(new { Mode = mode, Passed = exit == 0,
            DispatcherShutdownFinished = app.Dispatcher.HasShutdownFinished, CleanupPolicyClosed = policy.IsClosed,
            Runtime = Environment.Version.ToString(), AutomationWarmup = automationWarmup, Result = result, Error = failure?.ToString(),
            AfterDispatcherShutdown = MemoryTelemetry.ProcessSnapshot() },
            new JsonSerializerOptions { WriteIndented = true }));
        return exit;
    }
}
