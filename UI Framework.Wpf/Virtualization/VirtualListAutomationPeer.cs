using System.Windows.Automation.Peers;

namespace UI_Framework.Wpf;

// Let WPF own ItemContainer discovery, weak peer caching, scrolling and realization.
internal sealed class VirtualListAutomationPeer(VirtualListControl owner) : ItemsControlAutomationPeer(owner)
{
    protected override ItemAutomationPeer CreateItemAutomationPeer(object item) => new VirtualRowAutomationPeer(item, this);
    protected override string GetClassNameCore() => "VirtualList";
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.List;
}
