using System.Windows.Automation.Peers;

namespace UI_Framework.Wpf;

internal sealed class IconAutomationPeer(IconElement owner) : FrameworkElementAutomationPeer(owner)
{
    protected override string GetClassNameCore() => "Icon";
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Image;
    protected override bool IsControlElementCore() => !string.IsNullOrWhiteSpace(GetNameCore());
    protected override bool IsContentElementCore() => IsControlElementCore();
}
