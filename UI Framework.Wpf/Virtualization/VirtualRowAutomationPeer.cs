using System.Windows.Automation.Peers;

namespace UI_Framework.Wpf;

internal sealed class VirtualRowAutomationPeer(object item, ItemsControlAutomationPeer parent) : ItemAutomationPeer(item, parent)
{
    // Descriptions exist before native realization. Do not mount rows to obtain a name.
    // ItemAutomationPeer keeps the item weakly; do not capture it in another field.
    protected override string GetNameCore() => Item is VirtualRow row
        ? row.View.AccessibleName ?? row.View.Content
        : string.Empty;
    protected override string GetClassNameCore() => "VirtualRow";
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ListItem;
}
