using System.Runtime.InteropServices;
using System.Text.Json;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using UI_Framework.Wpf;

internal static class Program
{
    private static int checks;
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        checks++;
    }

    [STAThread]
    private static int Main(string[] args)
    {
        var app = new Application();
        var failCleanup = false;
        Action? duringCleanup = null;
        var depth = 0;
        var maxDepth = 0;
        var policy = new ApplicationComCleanupPolicy(app.Dispatcher, () =>
        {
            depth++;
            maxDepth = Math.Max(maxDepth, depth);
            try
            {
                if (failCleanup) { failCleanup = false; throw new IOException("Injected cleanup failure"); }
                var callback = duringCleanup; duringCleanup = null; callback?.Invoke();
                Marshal.CleanupUnusedObjectsInCurrentContext();
            }
            finally { depth--; }
        });
        app.Dispatcher.InvokeAsync(async () =>
        {
            try
            {
                var before = policy.CleanupCount;
                try { policy.RunUpdate(() => throw new IOException("Injected application failure")); }
                catch (IOException) { }
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                Check(policy.CleanupCount > before, "Exception did not release the update boundary.");
                before = policy.CleanupCount;
                policy.RequestCleanup();
                policy.RunUpdate(() =>
                {
                    policy.RequestCleanup(); policy.RequestCleanup();
                    var frame = new DispatcherFrame();
                    app.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => frame.Continue = false));
                    Dispatcher.PushFrame(frame);
                    Check(policy.CleanupCount == before, "Cleanup reentered an active update.");
                    try { policy.CloseBeforeDispatcherShutdown(); throw new Exception("Close should reject active updates."); }
                    catch (InvalidOperationException) { checks++; }
                });
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                Check(policy.CleanupCount == before + 1, "Cleanup requests were not coalesced.");
                duringCleanup = () => policy.RunUpdate(policy.RequestCleanup);
                policy.RequestCleanup();
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                Check(maxDepth == 1, "Cleanup recursively entered itself.");
                var wrongThreadRejected = await Task.Run(() =>
                {
                    try { policy.RequestCleanup(); return false; }
                    catch (InvalidOperationException) { return true; }
                });
                Check(wrongThreadRejected, "Cross-thread cleanup was accepted.");
                failCleanup = true; policy.RequestCleanup();
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                Check(policy.LastError is IOException, "Cleanup failure was hidden.");
                policy.RequestCleanup();
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                Check(policy.LastError is null, "Cleanup did not recover after failure.");

                var created = 0; var released = 0; TextBox? editor = null;
                using (var host = new ViewHost(() => WpfUI.Native(
                    () => { created++; return editor = new TextBox { Text = "retained text", AcceptsReturn = true }; },
                    release: text => { Check(text.Parent is null, "Release preceded detachment."); released++; }).Id("editor")))
                using (var source = new HwndSource(new HwndSourceParameters("Cleanup lifecycle check")
                { Width = 600, Height = 400, PositionX = -10000, PositionY = -10000, WindowStyle = unchecked((int)0x80000000) }))
                {
                    source.RootVisual = host;
                    host.Measure(new Size(600, 400)); host.Arrange(new Rect(0, 0, 600, 400));
                    editor!.Select(2, 4);
                    for (var round = 0; round < 10; round++)
                    {
                        policy.RunUpdate(host.Refresh);
                        var wrappers = Enumerable.Range(0, 100).Select(_ => NativeProbe.CreateWrapper()).ToArray();
                        Check(NativeProbe.ActiveProbes() == 100, "Native COM fixture count differs.");
                        // Forced GC is a lifetime check, outside any performance measurement.
                        GC.Collect(); GC.WaitForPendingFinalizers();
                        policy.RequestCleanup();
                        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                        Check(NativeProbe.ActiveProbes() == 0, "COM objects survived explicit cleanup.");
                        Check(wrappers.All(reference => !reference.IsAlive), "Managed wrappers remained rooted.");
                    }
                    Check(created == 1 && released == 0, "Cleanup recreated/released a retained island.");
                    Check(editor.SelectionStart == 2 && editor.SelectionLength == 4 && editor.Text == "retained text", "Editor state changed.");
                    Check(InputMethod.GetIsInputMethodEnabled(editor), "Input methods were disabled.");
                }
                Check(released == 1, "Native host release count differs.");
                Check(NativeProbe.WrongThreadReleases() == 0, "COM release happened off the owning STA.");
                var shutdownWrappers = Enumerable.Range(0, 100).Select(_ => NativeProbe.CreateWrapper()).ToArray();
                GC.Collect(); GC.WaitForPendingFinalizers();
                policy.RequestCleanup();
                failCleanup = true;
                try { policy.CloseBeforeDispatcherShutdown(); throw new Exception("Shutdown failure should be reported."); }
                catch (InvalidOperationException) { Check(!policy.IsClosed, "Failed shutdown prevented retry."); }
                policy.CloseBeforeDispatcherShutdown();
                Check(policy.IsClosed && policy.LastError is null, "Shutdown cleanup failed.");
                Check(NativeProbe.ActiveProbes() == 0 && NativeProbe.WrongThreadReleases() == 0, "Shutdown leaked or released on the wrong thread.");
                Check(shutdownWrappers.All(reference => !reference.IsAlive), "Shutdown wrappers remained rooted.");
                before = policy.CleanupCount;
                policy.CloseBeforeDispatcherShutdown(); policy.RequestCleanup();
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                Check(policy.CleanupCount == before, "Cleanup continued after owner closed.");
                File.WriteAllText(args[0], JsonSerializer.Serialize(new { Checks = checks, NativeObjects = 1100, Remaining = NativeProbe.ActiveProbes(), WrongThreadReleases = NativeProbe.WrongThreadReleases(), policy.CleanupCount, DiagnosticOnly = true }));
                Console.WriteLine($"Passed {checks} lifecycle checks; 1,100 COM objects released on their owning STA.");
                app.Shutdown();
            }
            catch (Exception error) { Console.Error.WriteLine(error); app.Shutdown(1); }
        });
        return app.Run();
    }
}
