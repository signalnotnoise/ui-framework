using System.Diagnostics;
using System.IO;
using Microsoft.Web.WebView2.Core;

namespace WebViewAcceptance;

internal static class ProcessFault
{
    internal static int TerminateOwnedProcess(BrowserSession owner, string expectedProfile, bool browser)
    {
        var environment = owner.Environment!;
        Probe.Require(string.Equals(Path.GetFullPath(environment.UserDataFolder), Path.GetFullPath(expectedProfile),
            StringComparison.OrdinalIgnoreCase), "Refusing fault injection outside this isolated profile.");
        var kind = browser ? CoreWebView2ProcessKind.Browser : CoreWebView2ProcessKind.Renderer;
        var targets = environment.GetProcessInfos().Where(info => info.Kind == kind).ToArray();
        Probe.Require(targets.Length == 1, "Expected exactly one owned process for fault injection: " + kind);
        var pid = targets[0].ProcessId;
        if (browser) Probe.Require(pid == owner.Browser.CoreWebView2.BrowserProcessId, "Environment/core browser PID mismatch.");
        using var process = Process.GetProcessById((int)pid);
        _ = process.SafeHandle; // Retain the process handle rather than later targeting a recycled PID.
        Probe.Require(process.ProcessName.Equals("msedgewebview2", StringComparison.OrdinalIgnoreCase), "Unexpected process executable.");
        Console.WriteLine($"Terminating isolated {kind} PID {pid} for profile {expectedProfile}");
        process.Kill(); // Only this verified process, never a name-wide or process-tree kill.
        return pid;
    }
}
