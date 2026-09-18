using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

[TestClass]
public sealed class AccessibilityTests
{
    [TestMethod]
    public void ExplicitNameUpdatesAndRemovalRestoresNativeContentWithoutReplacingButton() => StaTestRunner.Run(() =>
    {
        var label = new State<string?>("Inspect Evidence t2.e1");
        var clicks = 0;
        using var host = new ViewHost(() => Button("Inspect", () => clicks++).AccessibilityLabel(label.Value));
        TestVisualTree.Layout(host);
        var button = TestVisualTree.Find<Button>(host).Single();
        var peer = new ButtonAutomationPeer(button);
        Assert.AreEqual("Inspect Evidence t2.e1", peer.GetName());
        Assert.AreEqual("Inspect", button.Content);
        Assert.IsTrue(button.Focusable);
        Assert.AreEqual(AutomationControlType.Button, peer.GetAutomationControlType());
        button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
        Assert.AreEqual(1, clicks);
        label.Value = "Inspect Evidence t2.e2";
        TestVisualTree.Flush();
        Assert.AreSame(button, TestVisualTree.Find<Button>(host).Single());
        Assert.AreEqual("Inspect Evidence t2.e2", peer.GetName());
        label.Value = null;
        TestVisualTree.Flush();
        Assert.AreEqual(DependencyProperty.UnsetValue, button.ReadLocalValue(AutomationProperties.NameProperty));
        Assert.AreEqual("Inspect", peer.GetName());
    });
}
