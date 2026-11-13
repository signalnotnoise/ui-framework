using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;

namespace WebViewAcceptance;

internal static class RetentionChecks
{
    private const int CycleCount = 44;
    private const int WarmupCount = 4;
    internal static async Task<object> Run(string mode, string profile, string output)
    {
        var samples = new List<object>();
        // Detect a known retained owner before trusting zero-survivor measurements.
        var negative = await Cycle(profile + "-negative", true);
        await Collect();
        var retainedBeforeRelease = negative.Survivors();
        Probe.Require(negative.References["parent-owner"].IsAlive, "Negative control did not detect the retained owner.");
        negative.DeliberateRoot = null;
        await Collect();
        Probe.Require(negative.Survivors().Length == 0, "Negative-control release retained: " + string.Join(",", negative.Survivors()));
        for (var cycle = 0; cycle < CycleCount; cycle++)
        {
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            var timer = Stopwatch.StartNew();
            var result = await Cycle(mode == "retention-reused" ? profile : profile + "-" + cycle, false);
            timer.Stop();
            allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
            var natural = RetentionTelemetry.Read();
            await Collect();
            var survivors = result.Survivors();
            var sample = new { Cycle = cycle, Warmup = cycle < WarmupCount, result.Evidence, Survivors = survivors,
                Natural = natural, Collected = RetentionTelemetry.Read(), Milliseconds = timer.Elapsed.TotalMilliseconds,
                UiAllocatedBytes = allocated };
            samples.Add(sample);
            File.WriteAllText(Path.Combine(output, $"retention-{cycle}.json"), JsonSerializer.Serialize(sample, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"Retention cycle {cycle}: survivors={string.Join(',', survivors)}; {timer.Elapsed.TotalMilliseconds:F0} ms");
            Probe.Require(survivors.Length == 0, "Closed browser resources retained: " + string.Join(",", survivors));
        }
        return new { Passed = true, Mode = mode, NegativeControlDetectedAndReleased = true, Cycles = CycleCount,
            NegativeControl = new { negative.Evidence, RetainedBeforeRelease = retainedBeforeRelease, SurvivorsAfterRelease = negative.Survivors() },
            Warmups = WarmupCount, Samples = samples, ReleaseGate = false,
            Scope = "Managed collectability and normal isolated process exit; heap/private bytes are observations, not universal memory budgets." };
    }

    private static async Task Collect()
    {
        // Drain WPF/native callbacks before and after finalizers, with a fixed probe budget.
        for (var pass = 0; pass < 3; pass++)
        {
            await Probe.Flush();
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            await Probe.Flush();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<RetentionCycle> Cycle(string profile, bool negative)
    {
        var result = new RetentionCycle();
        var owner = new BrowserSession { ExpectedPage = "parent.html" };
        await Probe.Await(owner.Start(profile));
        owner.Browser.CoreWebView2.Navigate(new Uri(Path.Combine(AppContext.BaseDirectory, "Pages", "parent.html")).AbsoluteUri);
        await Probe.Await(owner.PageLoaded.Task);
        await Probe.Await(owner.Browser.ExecuteScriptAsync("openChild()"));
        await Probe.Await(owner.PopupStarted.Task);
        await Probe.Await(Task.WhenAll(owner.PopupTasks));
        var child = owner.Children.Single();
        await Probe.Await(child.PageLoaded.Task);
        Probe.Require(await child.Browser.ExecuteScriptAsync("document.title") == "\"Framework local child\"", "Popup page did not load.");
        foreach (var (name, session) in new[] { ("parent", owner), ("child", child) })
        {
            result.Track(name + "-owner", session); result.Track(name + "-window", session.Window);
            result.Track(name + "-host", session.Host); result.Track(name + "-browser", session.Browser);
            result.Track(name + "-core", session.Browser.CoreWebView2);
        }
        var environment = owner.Environment!;
        result.Track("environment", environment);
        var pid = owner.Browser.CoreWebView2.BrowserProcessId;
        var exited = new TaskCompletionSource<uint>(TaskCreationOptions.RunContinuationsAsynchronously);
        void Exited(object? sender, CoreWebView2BrowserProcessExitedEventArgs args) => exited.TrySetResult(args.BrowserProcessId);
        var processes = new List<Process>();
        environment.BrowserProcessExited += Exited;
        try
        {
            // Only inspect processes reported by this exact isolated environment; never kill them.
            Probe.Require(Path.GetFullPath(environment.UserDataFolder) == Path.GetFullPath(profile), "Unexpected profile.");
            foreach (var info in environment.GetProcessInfos())
            {
                var process = Process.GetProcessById(info.ProcessId);
                processes.Add(process);
                _ = process.SafeHandle; // Keep identity stable across exit/PID reuse.
            }
            Probe.Require(processes.Any(process => process.Id == pid), "Browser process was not captured.");
            var live = processes.Select(process => new { process.Id, process.PrivateMemorySize64, process.WorkingSet64 }).ToArray();
            await Probe.Await(owner.RequestClose()); owner.AssertClosed();
            Probe.Require(await exited.Task.WaitAsync(TimeSpan.FromSeconds(30)) == pid, "Wrong browser exit event.");
            await Task.WhenAll(processes.Select(process => process.WaitForExitAsync())).WaitAsync(TimeSpan.FromSeconds(30));
            Probe.Require(processes.All(process => process.HasExited), "An owned browser process survived normal close.");
            Probe.Require(environment.GetProcessInfos().Count == 0, "Environment reports processes after final browser exit.");
            result.Evidence = new { Runtime = environment.BrowserVersionString, Profile = profile, BrowserPid = pid,
                Processes = live, AllCapturedProcessesExited = true, RemainingProcesses = 0,
                ParentReleases = owner.Releases, ChildReleases = child.Releases, owner.DeferralsCompleted };
        }
        finally
        {
            environment.BrowserProcessExited -= Exited;
            foreach (var process in processes) process.Dispose();
        }
        if (negative) result.DeliberateRoot = owner;
        return result;
    }
}
