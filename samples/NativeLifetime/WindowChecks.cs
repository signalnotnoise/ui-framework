using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using UI_Framework.Wpf;
using static UI_Framework.UI;

namespace NativeLifetime;

internal static class WindowChecks
{
    internal static async Task<object> Run()
    {
        var counters = new LifetimeCounters();
        var window = ProbeUi.Window("Framework native window / focus / popup verification");
        new WindowInteropHelper(window).EnsureHandle();
        LifetimeEditor? editor = null;
        using var host = new ViewHost(() => WpfUI.Native(() => editor = new(counters),
            release: input => input.Dispose()).Height(180));
        var root = new Grid { ClipToBounds = true };
        root.Children.Add(host); window.Content = root;
        window.Show(); window.Activate();
        await ProbeUi.Flush(window);
        await editor!.Completion;
        ProbeUi.Require(PresentationSource.FromVisual(editor) is HwndSource, "Native editor lacks a real presentation source.");
        Keyboard.Focus(editor);
        ProbeUi.Require(editor.IsKeyboardFocused, "Keyboard focus did not reach the native editor; run on an interactive desktop.");
        editor.Text = "retained selection"; editor.Select(2, 5);
        host.Refresh(); await ProbeUi.Flush(window);
        ProbeUi.Require(editor.IsKeyboardFocused && editor.SelectionStart == 2 && editor.SelectionLength == 5,
            "Retained update lost native focus or selection.");
        editor.Popup.IsOpen = true; await ProbeUi.Flush(window);
        ProbeUi.Require(editor.Popup.IsOpen && PresentationSource.FromVisual(editor.Popup.Child) is HwndSource,
            "Child popup did not create its presentation source.");
        var initialWidth = editor.ActualWidth;
        window.Width = 820; await ProbeUi.Flush(window);
        ProbeUi.Require(editor.ActualWidth > initialWidth, "Native editor did not resize with the window.");
        var overlay = new Border { Width = 80, Height = 24, Background = Brushes.Orange,
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        root.Children.Add(overlay); await ProbeUi.Flush(window);
        var hit = VisualTreeHelper.HitTest(root, new Point(10, 10))?.VisualHit;
        ProbeUi.Require(ReferenceEquals(hit, overlay), "Managed overlay did not cover the WPF island.");
        var popupChild = editor.Popup.Child;
        host.Dispose(); await ProbeUi.Flush(window);
        ProbeUi.Require(!editor.Popup.IsOpen && editor.Popup.Child is null && editor.Popup.PlacementTarget is null,
            "Release retained the child popup or placement target.");
        ProbeUi.Require(PresentationSource.FromVisual(popupChild) is null, "Closed popup retained its presentation source.");
        window.Content = null; window.Close();
        ProbeUi.Require(counters.Created == counters.Released && counters.NativeBytes == 0, "Native resource imbalance after window checks.");
        var outcomes = new List<object>();
        foreach (var mode in new[] { "late-success", "late-fault", "cancel", "throw-cancel", "timeout-retry" })
            outcomes.Add(await ShutdownCase(mode));
        return new { Focus = true, RetainedSelection = true, PopupOwnership = true, Resize = true,
            ManagedOverlay = true, NativeAirspaceTested = false, ShutdownCases = outcomes };
    }

    private static async Task<object> ShutdownCase(string mode)
    {
        var counters = new LifetimeCounters();
        var pending = new TaskCompletionSource<NativeBuffer>(TaskCreationOptions.RunContinuationsAsynchronously);
        var window = ProbeUi.Window("Native shutdown verification: " + mode);
        new WindowInteropHelper(window).EnsureHandle();
        LifetimeEditor? editor = null;
        using var host = new ViewHost(() => WpfUI.Native(() => editor = new(counters, token =>
        {
            if (mode == "cancel") token.Register(() => pending.TrySetCanceled(token));
            if (mode == "throw-cancel") token.Register(() => throw new InvalidOperationException("cancellation callback failed"));
            return pending.Task;
        }), release: input => input.Dispose()));
        var owner = new NativeWindowSession(window, host, editor!.Completion,
            TimeSpan.FromMilliseconds(mode == "timeout-retry" ? 100 : 10000));
        window.Show(); await ProbeUi.Flush(window);
        NativeBuffer? lease = null;
        if (mode == "throw-cancel") { pending.SetResult(lease = new(counters)); await editor.Completion; }
        window.Close();
        await ProbeUi.Flush(window);
        var timeoutObserved = false;
        if (mode == "timeout-retry")
        {
            try { await owner.CloseCompletion!; }
            catch (TimeoutException) { timeoutObserved = true; }
            ProbeUi.Require(timeoutObserved && window.IsVisible, "Unsettled provider was reported as safely closed.");
        }
        if (mode is "late-success" or "timeout-retry") pending.SetResult(lease = new(counters));
        if (mode == "late-fault") pending.SetException(new InvalidOperationException("late provider fault"));
        if (mode == "timeout-retry") { await editor.Completion; window.Close(); }
        var cleanupFailureObserved = false;
        try { await owner.CloseCompletion!; }
        catch (AggregateException error) when (mode == "throw-cancel")
        {
            cleanupFailureObserved = error.Flatten().InnerExceptions.Any(x => x.Message == "cancellation callback failed");
        }
        ProbeUi.Require(!window.IsVisible && PresentationSource.FromVisual(host) is null, "Window did not detach and close after draining.");
        ProbeUi.Require(counters.Created == 1 && counters.Released == 1 && counters.NativeBytes == 0
            && counters.WrongThreadReleases == 0, "Shutdown leaked or released resources on the wrong thread.");
        ProbeUi.Require(counters.Errors.Count == (mode == "late-fault" ? 1 : 0), "Initialization error was lost or unexpected.");
        ProbeUi.Require(mode != "throw-cancel" || cleanupFailureObserved, "Throwing cancellation callback was swallowed.");
        ProbeUi.Require(mode == "throw-cancel" || !editor.Ready, "Late initialization modified a disposed editor.");
        ProbeUi.Require(lease is null || lease.DisposeCount == 1, "Late native allocation was not released exactly once.");
        return new { Mode = mode, Passed = true, TimeoutDeferredClose = timeoutObserved,
            CleanupFailureObserved = cleanupFailureObserved, ObservedInitializationErrors = counters.Errors };
    }
}
