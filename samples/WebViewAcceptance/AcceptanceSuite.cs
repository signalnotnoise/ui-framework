using System.IO;

namespace WebViewAcceptance;

internal static class AcceptanceSuite
{
    internal static async Task<object> Run(string mode, string profile)
    {
        var owner = new BrowserSession { HoldEnvironment = mode == "environment-boundary-close", ExpectedPage = "parent.html" };
        var initialization = owner.Start(profile);
        bool? popupInitializationPendingAtClose = null;
        if (mode == "environment-boundary-close")
        {
            await Probe.Await(owner.EnvironmentStarted.Task);
            if (initialization.IsFaulted) await initialization;
            Probe.Require(!initialization.IsCompleted, "Environment-to-control boundary was not held.");
            var closing = owner.RequestClose();
            owner.EnvironmentGate.TrySetResult();
            await Probe.Await(closing);
            Probe.Require(!owner.ControlStarted.Task.IsCompleted, "Closed owner started control initialization.");
        }
        else if (mode == "control-close")
        {
            await Probe.Await(owner.ControlStarted.Task);
            Probe.Require(owner.ControlWasPending && !initialization.IsCompleted, "Control initialization race not exercised.");
            await Probe.Await(owner.RequestClose());
        }
        else
        {
            await Probe.Await(initialization);
            var original = owner.Browser.CoreWebView2;
            owner.Host.Refresh();
            Probe.Require(ReferenceEquals(original, owner.Browser.CoreWebView2), "Refresh replaced browser identity.");
            owner.Browser.CoreWebView2.Navigate(new Uri(Path.Combine(AppContext.BaseDirectory, "Pages", "parent.html")).AbsoluteUri);
            await Probe.Await(owner.PageLoaded.Task);
            Probe.Require(await owner.Browser.ExecuteScriptAsync("document.title") == "\"Framework local parent\"", "Local parent page was not loaded.");
            owner.HoldPopup = mode != "popup-success";
            // Do not await script completion before servicing the popup deferral.
            var script = owner.Browser.ExecuteScriptAsync("openChild(); 'requested'");
            await Probe.Await(owner.PopupStarted.Task);
            var child = owner.Children.Single();
            Task? close = null;
            if (mode is "popup-parent-close" or "popup-child-close")
            {
                popupInitializationPendingAtClose = !child.Initialization.IsCompleted;
                Probe.Require(popupInitializationPendingAtClose.Value, "Popup initialization already settled; close race not exercised.");
            }
            if (mode == "popup-parent-close") close = owner.RequestClose();
            if (mode == "popup-child-close") close = child.RequestClose();
            await Probe.Await(owner.PopupReady.Task);
            owner.PopupGate.TrySetResult();
            await Probe.Await(Task.WhenAll(owner.PopupTasks));
            if (mode == "popup-success")
            {
                await Probe.Await(child.PageLoaded.Task);
                Probe.Require(await child.Browser.ExecuteScriptAsync("document.title") == "\"Framework local child\"", "Local child page was not loaded.");
                Probe.Require(owner.Assignments == 1, "Successful popup was not assigned.");
            }
            else Probe.Require(owner.Assignments == 0, "Popup assigned after close was requested.");
            // Closing can abort a script; observe its outcome explicitly.
            try { await Probe.Await(script); }
            catch (Exception error) when (mode == "popup-parent-close" && error is not TimeoutException)
            { Console.WriteLine("Observed script interrupted by parent close: " + error.GetType().Name); }
            if (close is not null) await Probe.Await(close);
            await Probe.Await(owner.RequestClose());
            Probe.Require(owner.DeferralsCompleted == 1, "Popup deferral not completed exactly once.");
        }
        owner.AssertClosed();
        return new { Mode = mode, Passed = true, owner.EnvironmentWasPending, owner.ControlWasPending,
            owner.Releases, Children = owner.Children.Count, ChildReleases = owner.Children.Sum(child => child.Releases),
            owner.DeferralsCompleted, owner.Assignments, owner.LateAssignments, owner.Errors,
            Runtime = owner.Environment?.BrowserVersionString, Profile = profile,
            PopupInitializationPendingAtClose = popupInitializationPendingAtClose,
            PopupBoundary = "Real provider initialization, then controlled pre-assignment continuation",
            Policy = "Defer close and drain provider work before detach/release; provider cancellation is not assumed" };
    }
}
