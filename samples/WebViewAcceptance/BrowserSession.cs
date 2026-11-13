using System.ComponentModel;
using System.IO;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using UI_Framework.Wpf;

namespace WebViewAcceptance;

// Test application ownership, not a framework browser API. The provider is drained,
// not assumed cancellable; the dispatcher stays alive until release completes.
internal sealed class BrowserSession
{
    internal readonly WebView2 Browser = new();
    internal readonly Window Window = new() { Width = 640, Height = 420, ShowActivated = false,
        Title = "Framework WebView2 acceptance — local pages" };
    internal readonly ViewHost Host;
    internal readonly List<BrowserSession> Children = [];
    internal readonly List<Task> PopupTasks = [];
    internal readonly List<string> Errors = [];
    internal readonly List<string> Timeouts = [];
    internal readonly TaskCompletionSource ScriptCloseRequested = Probe.Signal();
    internal Func<Task<CoreWebView2Environment>>? EnvironmentFactory;
    internal readonly TaskCompletionSource EnvironmentStarted = Probe.Signal();
    internal readonly TaskCompletionSource EnvironmentGate = Probe.Signal();
    internal readonly TaskCompletionSource ControlStarted = Probe.Signal();
    internal readonly TaskCompletionSource PopupStarted = Probe.Signal();
    internal readonly TaskCompletionSource PopupReady = Probe.Signal();
    internal readonly TaskCompletionSource PageLoaded = Probe.Signal();
    internal readonly TaskCompletionSource PopupGate = Probe.Signal();
    internal CoreWebView2Environment? Environment;
    internal Task Initialization { get; private set; } = Task.CompletedTask;
    internal Task? CloseCompletion { get; private set; }
    internal bool Closing { get; private set; }
    internal bool HoldPopup;
    internal bool HoldEnvironment;
    internal string ExpectedPage = "child.html";
    internal bool EnvironmentWasPending, ControlWasPending;
    internal int Releases, DeferralsCompleted, Assignments, LateAssignments;
    private bool allowClose;
    private CoreWebView2? subscribedCore;
    private bool started;
    internal int ScriptCloseRequests;

    internal BrowserSession(bool activate = false)
    {
        Host = new ViewHost(() => WpfUI.Native(() => Browser, release: browser =>
        {
            Probe.Require(browser.Parent is null, "Native release preceded detachment.");
            Releases++;
            browser.Dispose();
        }).Id("browser"));
        Window.Content = Host;
        Window.Closing += OnClosing;
        Window.ShowActivated = activate;
        Window.Show();
    }

    internal Task Start(string profile, CoreWebView2Environment? shared = null, string? runtimeFolder = null)
    {
        if (started || Closing) throw new InvalidOperationException("Create a fresh owner to retry initialization.");
        started = true;
        return Initialization = Initialize(profile, shared, runtimeFolder);
    }

    internal async Task StartWithin(string profile, TimeSpan timeout)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);
        var pending = Start(profile);
        try { await pending.WaitAsync(timeout); }
        catch (TimeoutException) when (!pending.IsCompleted)
        {
            Timeouts.Add("Initialization timed out; close is draining the pending provider operation.");
            _ = RequestClose();
            throw;
        }
    }

    internal async Task CloseWithin(TimeSpan timeout)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);
        var pending = RequestClose();
        try { await pending.WaitAsync(timeout); }
        catch (TimeoutException) when (!pending.IsCompleted)
        {
            Timeouts.Add("Close timed out; owner and dispatcher must remain alive until drain completes.");
            throw;
        }
    }

    private async Task Initialize(string profile, CoreWebView2Environment? shared, string? runtimeFolder)
    {
        try
        {
            // Reject a known invalid configuration before entering provider initialization,
            // which can otherwise stall rather than consistently returning an error.
            if (File.Exists(profile)) throw new IOException("A file occupies the browser profile directory: " + profile);
            var environment = EnvironmentFactory is { } factory ? factory() : shared is null
                ? CoreWebView2Environment.CreateAsync(browserExecutableFolder: runtimeFolder, userDataFolder: profile)
                : Task.FromResult(shared);
            EnvironmentWasPending = !environment.IsCompleted;
            EnvironmentStarted.TrySetResult();
            if (HoldEnvironment) await EnvironmentGate.Task;
            Environment = await environment;
            Probe.Require(string.Equals(Path.GetFullPath(Environment.UserDataFolder), Path.GetFullPath(profile),
                StringComparison.OrdinalIgnoreCase), "WebView2 did not use the isolated profile.");
            if (Closing) return;
            var control = Browser.EnsureCoreWebView2Async(Environment);
            ControlWasPending = !control.IsCompleted;
            ControlStarted.TrySetResult();
            await control;
            if (Closing) return;
            subscribedCore = Browser.CoreWebView2;
            subscribedCore.NewWindowRequested += OnPopup;
            subscribedCore.NavigationStarting += OnNavigation;
            subscribedCore.NavigationCompleted += OnLoaded;
            subscribedCore.ProcessFailed += OnProcessFailed;
            subscribedCore.WindowCloseRequested += OnScriptClose;
        }
        catch (Exception error)
        {
            Errors.Add("Initialization: " + error);
            Console.Error.WriteLine("Initialization failed: " + error);
            throw;
        }
    }

    private void OnNavigation(object? sender, CoreWebView2NavigationStartingEventArgs args)
    {
        var pages = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Pages")) + Path.DirectorySeparatorChar;
        if (!Uri.TryCreate(args.Uri, UriKind.Absolute, out var uri) || !uri.IsFile
            || !Path.GetFullPath(uri.LocalPath).StartsWith(pages, StringComparison.OrdinalIgnoreCase))
        {
            args.Cancel = true;
            Errors.Add("Blocked non-local navigation: " + args.Uri);
        }
    }
    private void OnLoaded(object? sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        if (Browser.Source is not { IsFile: true } source || Path.GetFileName(source.LocalPath) != ExpectedPage) return;
        if (args.IsSuccess) PageLoaded.TrySetResult();
        else PageLoaded.TrySetException(new InvalidOperationException("Navigation: " + args.WebErrorStatus));
    }
    private void OnProcessFailed(object? sender, CoreWebView2ProcessFailedEventArgs args)
        => Errors.Add("Process failed: " + args.ProcessFailedKind);

    private void OnScriptClose(object? sender, object args)
    {
        ScriptCloseRequests++;
        // CloseAsync yields out of the native event before disposing the controller.
        _ = RequestClose();
        ScriptCloseRequested.TrySetResult();
    }

    private void OnPopup(object? sender, CoreWebView2NewWindowRequestedEventArgs args)
    {
        var deferral = args.GetDeferral();
        args.Handled = true;
        PopupTasks.Add(OpenChild(args, deferral));
    }
    private async Task OpenChild(CoreWebView2NewWindowRequestedEventArgs args, CoreWebView2Deferral deferral)
    {
        try
        {
            if (Closing) return;
            var child = new BrowserSession();
            child.Window.Owner = Window;
            Children.Add(child);
            var initialization = child.Start(Environment!.UserDataFolder, Environment);
            PopupStarted.TrySetResult();
            await initialization;
            PopupReady.TrySetResult();
            // Deterministic app-continuation boundary after real provider initialization.
            if (HoldPopup) await PopupGate.Task;
            if (Closing || child.Closing) return;
            Probe.Require(ReferenceEquals(Environment, child.Environment), "Popup used another environment.");
            args.NewWindow = child.Browser.CoreWebView2;
            Assignments++;
            if (Closing || child.Closing) LateAssignments++;
        }
        catch (Exception error) { Errors.Add("Popup: " + error); }
        finally { deferral.Complete(); DeferralsCompleted++; }
    }

    private void OnClosing(object? sender, CancelEventArgs args)
    {
        if (allowClose) return;
        args.Cancel = true;
        Closing = true;
        CloseCompletion ??= CloseAsync();
    }
    internal Task RequestClose()
    {
        if (CloseCompletion is { } pending) return pending;
        Window.Close();
        return CloseCompletion ?? Task.CompletedTask;
    }
    private async Task CloseAsync()
    {
        await Probe.Flush(); // Exit the synchronous WPF Closing event.
        List<Exception> failures = [];
        async Task Drain(Task task) { try { await task; } catch (Exception error) { failures.Add(error); } }
        await Drain(Initialization);
        foreach (var child in Children) await Drain(child.RequestClose());
        await Drain(Task.WhenAll(PopupTasks));
        // Unsubscribe only after all possible event-producing work has drained.
        void Cleanup(Action action) { try { action(); } catch (Exception error) { failures.Add(error); } }
        Cleanup(() =>
        {
            // The control's CoreWebView2 getter throws after browser-process failure.
            // Use the object whose events this owner actually subscribed to.
            if (subscribedCore is not { } core) return;
            Cleanup(() => core.NewWindowRequested -= OnPopup);
            Cleanup(() => core.NavigationStarting -= OnNavigation);
            Cleanup(() => core.NavigationCompleted -= OnLoaded);
            Cleanup(() => core.ProcessFailed -= OnProcessFailed);
            Cleanup(() => core.WindowCloseRequested -= OnScriptClose);
        });
        subscribedCore = null;
        Cleanup(Host.Dispose);
        Window.Content = null;
        allowClose = true;
        Window.Closing -= OnClosing;
        Window.Close();
        if (failures.Count != 0) throw new AggregateException("Browser close failed.", failures);
    }

    internal void AssertClosed(int expectedErrors = 0)
    {
        Probe.Require(Closing && !Window.IsVisible && Releases == 1, "Browser owner did not close/release exactly once.");
        Probe.Require(Initialization.IsCompleted && PopupTasks.All(task => task.IsCompleted), "Owner left pending work.");
        Probe.Require(Errors.Count == expectedErrors && LateAssignments == 0, string.Join("\n", Errors));
        foreach (var child in Children) child.AssertClosed();
    }
}
