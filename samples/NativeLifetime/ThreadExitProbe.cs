using System.IO;
using System.Runtime.ExceptionServices;
using System.Text.Json;

namespace NativeLifetime;

// Explicit diagnostic: distinguish dispatcher shutdown from native STA thread exit.
internal static class ThreadExitProbe
{
    internal static int Run(Func<int> run, string reportPath)
    {
        var exit = 1;
        ExceptionDispatchInfo? failure = null;
        var thread = new Thread(() =>
        {
            try { exit = run(); }
            catch (Exception error) { failure = ExceptionDispatchInfo.Capture(error); }
        }) { Name = "Framework diagnostic UI thread" };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        failure?.Throw();
        using (var file = new FileStream(reportPath + ".thread-exit.json", FileMode.CreateNew))
            JsonSerializer.Serialize(file, new { ExitCode = exit, UiThreadExited = !thread.IsAlive,
                Snapshot = MemoryTelemetry.ProcessSnapshot(), ReleaseGate = false });
        Task.Run(() => MeasurementTools.MemoryCheckpoint.Pause("post-thread")).GetAwaiter().GetResult();
        return exit;
    }
}
