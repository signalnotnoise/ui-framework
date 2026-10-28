using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Interop;
using System.Windows.Threading;

internal static class TestAutomationTree
{
    // Call inside StaTestRunner: its timeout also bounds an unresponsive native UIA call.
    internal static void Initialize(Window window, string descendantClassName)
    {
        window.Dispatcher.VerifyAccess();
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero) throw new InvalidOperationException("Show the test window before initializing automation.");
        var dispatcher = window.Dispatcher;
        var frame = new DispatcherFrame();
        ExceptionDispatchInfo? failure = null;
        var client = new Thread(() =>
        {
            try
            {
                var root = AutomationElement.FromHandle(handle);
                var descendant = root.FindFirst(TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.ClassNameProperty, descendantClassName));
                if (descendant is null)
                    throw new InvalidOperationException($"Native UI Automation did not expose '{descendantClassName}' in the test window.");
            }
            catch (Exception error) { failure = ExceptionDispatchInfo.Capture(error); }
            finally { dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() => frame.Continue = false)); }
        }) { IsBackground = true, Name = "Test native UIA initialization" };
        client.SetApartmentState(ApartmentState.MTA);
        client.Start();
        // Native UIA calls back into this dispatcher. Blocking it with Join would deadlock.
        Dispatcher.PushFrame(frame);
        failure?.Throw();
        var peer = UIElementAutomationPeer.CreatePeerForElement(window)
            ?? throw new InvalidOperationException("The test window has no automation peer.");
        Refresh(peer);
    }

    private static void Refresh(AutomationPeer peer)
    {
        // Peers cached before the native query may still carry an uninitialized handle.
        peer.ResetChildrenCache();
        foreach (var child in peer.GetChildren() ?? []) Refresh(child);
    }
}
