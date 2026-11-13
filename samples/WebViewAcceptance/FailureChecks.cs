using System.IO;
using Microsoft.Web.WebView2.Core;

namespace WebViewAcceptance;

internal static class FailureChecks
{
    internal static async Task<object> Run(string mode, string profile)
        => mode switch
        {
            "initialization-recovery" => await Initialization(profile, false),
            "runtime-missing-recovery" => await Initialization(profile, true),
            "navigation-recovery" => await Navigation(profile),
            "renderer-recovery" => await ProcessFailure(profile, false),
            "browser-recovery" => await ProcessFailure(profile, true),
            _ => throw new ArgumentException("Unknown failure case.")
        };

    private static string Page(string name) => new Uri(Path.Combine(AppContext.BaseDirectory, "Pages", name)).AbsoluteUri;
    private static async Task<CoreWebView2NavigationCompletedEventArgs> Navigate(BrowserSession owner, string uri)
    {
        var completed = new TaskCompletionSource<CoreWebView2NavigationCompletedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        ulong? navigationId = null;
        void Starting(object? sender, CoreWebView2NavigationStartingEventArgs args)
        {
            if (args.Uri == uri) navigationId = args.NavigationId;
        }
        void Loaded(object? sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            if (args.NavigationId == navigationId) completed.TrySetResult(args);
        }
        var core = owner.Browser.CoreWebView2;
        core.NavigationStarting += Starting;
        core.NavigationCompleted += Loaded;
        try { core.Navigate(uri); return await completed.Task.WaitAsync(TimeSpan.FromSeconds(30)); }
        finally { core.NavigationStarting -= Starting; core.NavigationCompleted -= Loaded; }
    }
    private static async Task VerifyPage(BrowserSession owner)
    {
        var navigation = await Navigate(owner, Page("parent.html"));
        Probe.Require(navigation.IsSuccess, "Recovery navigation failed: " + navigation.WebErrorStatus);
        Probe.Require(await owner.Browser.ExecuteScriptAsync("document.title") == "\"Framework local parent\"", "Recovery did not load the real local page.");
        owner.Host.Refresh();
        Probe.Require(await owner.Browser.ExecuteScriptAsync("document.getElementById('editor').value") == "\"retained input\"", "Recovered browser did not survive framework refresh.");
    }

    private static async Task<object> Initialization(string profile, bool missingRuntime)
    {
        // A file deliberately occupies this test's profile path. No existing data is changed.
        if (!missingRuntime) await File.WriteAllTextAsync(profile, "Intentional isolated-profile initialization failure");
        var failed = new BrowserSession { ExpectedPage = "parent.html" };
        Exception? observed = null;
        try { await Probe.Await(failed.Start(profile, runtimeFolder: missingRuntime ? profile + "-missing-runtime" : null)); }
        catch (Exception error) when (error is not TimeoutException) { observed = error; }
        Probe.Require(observed is not null && failed.Errors.Count == 1 && failed.Errors[0].StartsWith("Initialization:"), "Expected provider initialization failure was not reported.");
        if (missingRuntime) Probe.Require(observed is WebView2RuntimeNotFoundException, "Expected a real WebView2 missing-runtime exception.");
        else Probe.Require(observed is IOException && !failed.EnvironmentStarted.Task.IsCompleted, "Invalid profile was not rejected before provider initialization.");
        var closeReportedFailure = false;
        try { await Probe.Await(failed.RequestClose()); }
        catch (AggregateException error)
        {
            closeReportedFailure = error.Flatten().InnerExceptions is var failures && failures.Count == 1 && ReferenceEquals(failures[0], observed);
        }
        Probe.Require(closeReportedFailure, "Close lost the initialization error.");
        failed.AssertClosed(expectedErrors: 1);
        var recovered = new BrowserSession { ExpectedPage = "parent.html" };
        await Probe.Await(recovered.Start(profile + "-recovery"));
        await VerifyPage(recovered);
        await Probe.Await(recovered.RequestClose()); recovered.AssertClosed();
        return new { Passed = true, Failure = observed!.GetType().FullName, observed.HResult,
            FailureOrigin = missingRuntime ? "WebView2 provider missing-runtime failure" : "Application profile preflight (provider not called)",
            ErrorReported = true, CloseReportedFailure = closeReportedFailure, FailedReleases = failed.Releases,
            RecoveryReleases = recovered.Releases, Runtime = recovered.Environment!.BrowserVersionString,
            Policy = "Observe invalid profile failure, release failed owner, explicitly create a fresh owner with a valid isolated profile" };
    }

    private static async Task<object> Navigation(string profile)
    {
        var owner = new BrowserSession { ExpectedPage = "parent.html" };
        await Probe.Await(owner.Start(profile));
        var core = owner.Browser.CoreWebView2;
        var missing = Page("missing-" + Guid.NewGuid().ToString("N") + ".html");
        var failed = await Navigate(owner, missing);
        Console.WriteLine($"Missing-file navigation: success={failed.IsSuccess}; status={failed.WebErrorStatus}; id={failed.NavigationId}");
        // Local file failures may legitimately map to Unknown rather than a network error enum.
        Probe.Require(!failed.IsSuccess, "Missing local file did not report navigation failure.");
        await VerifyPage(owner);
        Probe.Require(ReferenceEquals(core, owner.Browser.CoreWebView2), "Navigation recovery replaced a healthy controller.");
        await Probe.Await(owner.RequestClose()); owner.AssertClosed();
        return new { Passed = true, NavigationFailure = failed.WebErrorStatus.ToString(), ControllerRetained = true,
            owner.Releases, Runtime = owner.Environment!.BrowserVersionString,
            Policy = "Observe NavigationCompleted failure and explicitly navigate to a valid local page" };
    }

    private static async Task<object> ProcessFailure(string profile, bool browserFailure)
    {
        var owner = new BrowserSession { ExpectedPage = "parent.html" };
        await Probe.Await(owner.Start(profile)); await VerifyPage(owner);
        var core = owner.Browser.CoreWebView2;
        var environment = owner.Environment!;
        var oldPid = core.BrowserProcessId;
        var failed = new TaskCompletionSource<CoreWebView2ProcessFailedKind>(TaskCreationOptions.RunContinuationsAsynchronously);
        var exited = new TaskCompletionSource<uint>(TaskCreationOptions.RunContinuationsAsynchronously);
        void Failed(object? sender, CoreWebView2ProcessFailedEventArgs args) => failed.TrySetResult(args.ProcessFailedKind);
        void Exited(object? sender, CoreWebView2BrowserProcessExitedEventArgs args) => exited.TrySetResult(args.BrowserProcessId);
        core.ProcessFailed += Failed; environment.BrowserProcessExited += Exited;
        try
        {
            var terminatedPid = ProcessFault.TerminateOwnedProcess(owner, profile, browserFailure);
            var observed = await failed.Task.WaitAsync(TimeSpan.FromSeconds(30));
            Probe.Require(observed == (browserFailure ? CoreWebView2ProcessFailedKind.BrowserProcessExited : CoreWebView2ProcessFailedKind.RenderProcessExited), "Unexpected process failure: " + observed);
            Probe.Require(owner.Errors.Count == 1 && owner.Errors[0] == "Process failed: " + observed, "Process failure was not reported exactly once.");
            if (!browserFailure)
            {
                await VerifyPage(owner);
                Probe.Require(ReferenceEquals(core, owner.Browser.CoreWebView2) && core.BrowserProcessId == oldPid, "Renderer recovery replaced the browser/controller.");
                core.ProcessFailed -= Failed;
                await Probe.Await(owner.RequestClose()); owner.AssertClosed(expectedErrors: 1);
                return new { Passed = true, Failure = observed.ToString(), TerminatedPid = terminatedPid,
                    ControllerRetained = true, owner.Releases, Runtime = environment.BrowserVersionString };
            }
            var exitedPid = await exited.Task.WaitAsync(TimeSpan.FromSeconds(30));
            Probe.Require(exitedPid == oldPid, "Exit event was not for the isolated test browser.");
            core.ProcessFailed -= Failed;
            await Probe.Await(owner.RequestClose()); owner.AssertClosed(expectedErrors: 1);
            var recovered = new BrowserSession { ExpectedPage = "parent.html" };
            await Probe.Await(recovered.Start(profile)); await VerifyPage(recovered);
            var newPid = recovered.Browser.CoreWebView2.BrowserProcessId;
            Probe.Require(newPid != oldPid, "Browser process was not recreated.");
            await Probe.Await(recovered.RequestClose()); recovered.AssertClosed();
            return new { Passed = true, Failure = observed.ToString(), TerminatedPid = terminatedPid,
                OldBrowserPid = oldPid, NewBrowserPid = newPid, ExitObservedBeforeRecovery = true,
                FailedReleases = owner.Releases, RecoveryReleases = recovered.Releases,
                Runtime = recovered.Environment!.BrowserVersionString,
                Policy = "Wait for isolated BrowserProcessExited, release failed owner, explicitly recreate with the same isolated profile" };
        }
        finally { environment.BrowserProcessExited -= Exited; }
    }
}
