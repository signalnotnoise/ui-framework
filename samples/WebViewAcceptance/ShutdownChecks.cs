using System.IO;
using Microsoft.Web.WebView2.Core;

namespace WebViewAcceptance;

internal static class ShutdownChecks
{
    internal static Task<object> Run(string mode, string profile) => mode.StartsWith("stalled-", StringComparison.Ordinal)
        ? Stall(profile, mode == "stalled-fault-close") : ScriptClose(profile, mode == "script-parent-close");

    private static async Task<object> Stall(string profile, bool fault)
    {
        var provider = new TaskCompletionSource<CoreWebView2Environment>(TaskCreationOptions.RunContinuationsAsynchronously);
        var owner = new BrowserSession { EnvironmentFactory = () => provider.Task };
        var deadline = TimeSpan.FromMilliseconds(100);
        try { await owner.StartWithin(profile, deadline); throw new InvalidOperationException("Missing initialization timeout."); }
        catch (TimeoutException) { }
        Probe.Require(owner.Closing && !owner.Initialization.IsCompleted && owner.Releases == 0, "Timeout abandoned pending provider work.");
        var closing = owner.CloseCompletion!;
        try { await owner.CloseWithin(deadline); throw new InvalidOperationException("Missing close timeout."); }
        catch (TimeoutException) { }
        Probe.Require(ReferenceEquals(closing, owner.RequestClose()) && !closing.IsCompleted && owner.Releases == 0,
            "Close retry replaced the drain or released a pending owner.");
        Probe.Require(owner.Timeouts.Count == 2 && owner.Window.IsVisible, "Timeout diagnostics/owner were lost.");
        var restartRejected = false;
        try { _ = owner.Start(profile); } catch (InvalidOperationException) { restartRejected = true; }
        Probe.Require(restartRejected, "Timed-out owner accepted another initialization.");
        var lateError = new IOException("Controlled late provider failure");
        if (fault) provider.SetException(lateError);
        else provider.SetResult(await CoreWebView2Environment.CreateAsync(userDataFolder: profile));
        var faultObserved = false;
        try { await Probe.Await(closing); }
        catch (AggregateException error) when (fault)
        {
            faultObserved = error.Flatten().InnerExceptions is var failures && failures.Count == 1 && ReferenceEquals(failures[0], lateError);
        }
        Probe.Require(!fault || faultObserved, "Late initialization fault was lost.");
        Probe.Require(!owner.ControlStarted.Task.IsCompleted, "Late initialization attached a controller after timeout.");
        owner.AssertClosed(fault ? 1 : 0);
        var recovered = new BrowserSession();
        await Probe.Await(recovered.Start(profile + "-retry"));
        await Probe.Await(recovered.RequestClose()); recovered.AssertClosed();
        return new { Passed = true, ControlledPendingProvider = true, LateFault = fault, FaultObserved = faultObserved,
            owner.Timeouts, owner.Releases, RetryReleases = recovered.Releases, RestartRejected = restartRejected,
            Policy = "Timeout bounds the caller wait, not provider lifetime; retain owner/dispatcher, drain late completion, retry with a fresh owner." };
    }

    private static async Task<object> ScriptClose(string profile, bool parentClose)
    {
        var owner = new BrowserSession { ExpectedPage = "parent.html" };
        await Probe.Await(owner.Start(profile));
        owner.Browser.CoreWebView2.Navigate(new Uri(Path.Combine(AppContext.BaseDirectory, "Pages", "parent.html")).AbsoluteUri);
        await Probe.Await(owner.PageLoaded.Task);
        await Probe.Await(owner.Browser.ExecuteScriptAsync("openChild()"));
        await Probe.Await(owner.PopupStarted.Task);
        await Probe.Await(Task.WhenAll(owner.PopupTasks));
        var child = owner.Children.Single();
        await Probe.Await(child.PageLoaded.Task);
        var target = parentClose ? owner : child;
        // Schedule after this script's result so normal controller disposal cannot abort the command.
        await Probe.Await(target.Browser.ExecuteScriptAsync("setTimeout(() => window.close(), 100); 'scheduled'"));
        await Probe.Await(target.ScriptCloseRequested.Task);
        await Probe.Await(target.CloseCompletion!);
        target.AssertClosed();
        Probe.Require(target.ScriptCloseRequests == 1, "Script close event was not handled exactly once.");
        if (!parentClose)
        {
            Probe.Require(!owner.Closing && owner.Window.IsVisible, "Child script close closed its parent.");
            Probe.Require(await owner.Browser.ExecuteScriptAsync("document.title") == "\"Framework local parent\"", "Parent stopped working after child close.");
            await Probe.Await(owner.RequestClose());
        }
        owner.AssertClosed();
        Probe.Require(owner.DeferralsCompleted == 1 && owner.Assignments == 1, "Popup ownership was incomplete.");
        return new { Passed = true, ParentRequestedClose = parentClose, target.ScriptCloseRequests,
            ParentReleases = owner.Releases, ChildReleases = child.Releases, owner.DeferralsCompleted,
            Runtime = owner.Environment!.BrowserVersionString };
    }
}
