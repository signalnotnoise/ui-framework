using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Interop;
using UI_Framework.Wpf;
using static UI_Framework.UI;

namespace NativeLifetime;

// Diagnostic ownership experiment, not a general UIA-client memory budget.
internal static class AutomationRetentionProbe
{
    private static AutomationPeer? held;
    private static AutomationPeer[] refreshTargets = [];

    internal static async Task WarmClient()
    {
        var window = ProbeUi.Window("Framework isolated automation warmup");
        var counters = new LifetimeCounters();
        window.Show();
        try
        {
            await MountAndDispose(window, counters, "query-release");
            await ProbeUi.Collect(window);
            ProbeUi.Require(counters.Alive().Values.All(count => count == 0)
                && counters.Created == counters.Released && counters.NativeBytes == 0,
                "Automation warmup retained tracked resources.");
        }
        finally { held = null; window.Content = null; window.Close(); }
    }

    internal static async Task<object> Run()
    {
        var results = new List<object>();
        foreach (var kind in new[] { "none", "query-release", "row", "list", "editor", "row-refresh", "rows-refresh" })
        {
            var window = ProbeUi.Window("Framework automation retention diagnostic");
            var counters = new LifetimeCounters();
            window.Show();
            try
            {
                await MountAndDispose(window, counters, kind);
                await ProbeUi.Collect(window);
                var retained = counters.Alive();
                var before = MemoryTelemetry.ProcessSnapshot();
                if (kind == "row-refresh") Refresh(held!);
                if (kind == "rows-refresh")
                {
                    foreach (var peer in refreshTargets) peer.ResetChildrenCache();
                    refreshTargets = [];
                }
                await ProbeUi.Collect(window);
                var refreshed = counters.Alive();
                held = null;
                await ProbeUi.Collect(window);
                await Task.Delay(250);
                await ProbeUi.Collect(window);
                var released = counters.Alive();
                ProbeUi.Require(counters.Created == counters.Released && counters.NativeBytes == 0,
                    "Native ownership did not balance while automation peers were held.");
                ProbeUi.Require(released.Values.All(count => count == 0),
                    "Tracked objects survived after releasing diagnostic automation roots: " + kind);
                results.Add(new { Kind = kind, Retained = retained, Refreshed = refreshed, Released = released,
                    BeforeRelease = before, AfterRelease = MemoryTelemetry.ProcessSnapshot(), counters.Created,
                    ReleaseCount = counters.Released, counters.NativeBytes });
            }
            finally { held = null; refreshTargets = []; window.Content = null; window.Close(); }
        }
        return new { Kind = "controlled-automation-peer-retention", ReleaseGate = false, Cases = results };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task MountAndDispose(Window window, LifetimeCounters counters, string kind)
    {
        using var host = new ViewHost(() => VirtualList(Enumerable.Range(0, 500)
            .Select(index => Component<MemoryRow>(row => row.Counters = counters).Id(index.ToString())
                .AccessibilityLabel($"Record {index}")), 280));
        counters.Track("host", host);
        window.Content = host;
        await ProbeUi.Flush(window);
        await ProbeUi.Find<LifetimeEditor>(host).First().Completion;
        if (kind != "none")
        {
            var handle = new WindowInteropHelper(window).Handle;
            // Query only this sample's HWND; never inspect another desktop application.
            await Task.Run(() =>
            {
                var root = AutomationElement.FromHandle(handle);
                ProbeUi.Require(root.FindFirst(TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.ClassNameProperty, "VirtualList")) is not null,
                    "Sample list was not exposed to the UIA client.");
            });
            var rootPeer = UIElementAutomationPeer.CreatePeerForElement(window)!;
            Refresh(rootPeer);
            var list = UIElementAutomationPeer.CreatePeerForElement(ProbeUi.Find<ItemsControl>(host).Single())!;
            var rows = list.GetChildren()!.Where(peer => peer.GetClassName() == "VirtualRow").ToArray();
            ProbeUi.Require(rows.Length > 0, "No realized row peers found.");
            foreach (var row in rows) _ = row.GetChildren();
            if (kind == "rows-refresh") refreshTargets = rows;
            held = kind == "query-release" ? null : kind == "list" ? list : kind == "editor"
                ? UIElementAutomationPeer.CreatePeerForElement(ProbeUi.Find<LifetimeEditor>(host).First())
                : rows[0];
        }
        host.Dispose(); window.Content = null;
        await ProbeUi.Flush(window);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Refresh(AutomationPeer peer)
    {
        foreach (var child in peer.GetChildren() ?? []) Refresh(child);
        peer.ResetChildrenCache();
    }
}
