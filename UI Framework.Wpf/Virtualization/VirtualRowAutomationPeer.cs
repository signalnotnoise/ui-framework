using System.Windows.Automation.Peers;
using System.Runtime.CompilerServices;

namespace UI_Framework.Wpf;

internal sealed class VirtualRowAutomationPeer : ItemAutomationPeer
{
    // Optional ephemeron storage keeps unqueried logical rows allocation-free and
    // never roots a row, peer or owning list. More than one live peer may name a row.
    private static ConditionalWeakTable<VirtualRow, List<WeakReference<VirtualRowAutomationPeer>>>? peers;

    internal VirtualRowAutomationPeer(object item, ItemsControlAutomationPeer parent) : base(item, parent)
    {
        if (item is VirtualRow row)
        {
            if (peers is null) Interlocked.CompareExchange(ref peers, new(), null);
            peers.GetOrCreateValue(row).Add(new(this));
        }
    }

    internal static bool RefreshChildren(VirtualRow row)
    {
        if (peers is null || !peers.TryGetValue(row, out var registered)) return false;
        for (var i = registered.Count - 1; i >= 0; i--)
        {
            if (registered[i].TryGetTarget(out var peer)) peer.ResetChildrenCache();
            else registered.RemoveAt(i);
        }
        return registered.Count != 0;
    }

    internal static bool HasPeers(VirtualRow row) => peers is not null
        && peers.TryGetValue(row, out var registered) && registered.Count != 0;

    // The generator can still associate a recycling container with the old item
    // during release. Never expose that container's stale actionable children.
    protected override List<AutomationPeer>? GetChildrenCore() => Item is VirtualRow { Presenter: not null }
        ? base.GetChildrenCore() : null;

    // Descriptions exist before native realization. Do not mount rows to obtain a name.
    // ItemAutomationPeer keeps the item weakly; do not capture it in another field.
    protected override string GetNameCore() => Item is VirtualRow row
        ? row.View.AccessibleName ?? row.View.Content
        : string.Empty;
    protected override string GetClassNameCore() => "VirtualRow";
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ListItem;
}
