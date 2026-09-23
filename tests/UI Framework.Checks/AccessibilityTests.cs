using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

[TestClass]
public sealed class AccessibilityTests
{
    [TestMethod]
    public void RealWindowExposesEditorToggleAndButtonPatternsUnderTheme() => StaTestRunner.Run(() =>
    {
        var text = new State<string>("editable");
        var enabled = new State<bool>(false);
        using var host = new ViewHost(() => VStack(
            TextField(text).AccessibilityLabel("Document title"),
            Toggle("Enabled", enabled), Button("Save", () => { })));
        ThemeStyles.Apply(host, new ThemeTokens());
        var window = new Window { Content = host, Width = 500, Height = 300, ShowInTaskbar = false };
        try
        {
            window.Show();
            TestVisualTree.Layout(host);
            var editor = TestVisualTree.Find<TextBox>(host).Single();
            var peer = UIElementAutomationPeer.CreatePeerForElement(editor)!;
            Assert.AreEqual("Document title", peer.GetName());
            Assert.AreEqual(AutomationControlType.Edit, peer.GetAutomationControlType());
            var value = (IValueProvider)peer.GetPattern(PatternInterface.Value)!;
            Assert.IsFalse(value.IsReadOnly);
            value.SetValue("changed through automation");
            Assert.AreEqual("changed through automation", text.Value);
            var togglePeer = UIElementAutomationPeer.CreatePeerForElement(TestVisualTree.Find<CheckBox>(host).Single())!;
            Assert.IsInstanceOfType<IToggleProvider>(togglePeer.GetPattern(PatternInterface.Toggle));
            ((IToggleProvider)togglePeer.GetPattern(PatternInterface.Toggle)!).Toggle();
            Assert.IsTrue(enabled.Value);
            var buttonPeer = UIElementAutomationPeer.CreatePeerForElement(TestVisualTree.Find<Button>(host).Single())!;
            Assert.IsInstanceOfType<IInvokeProvider>(buttonPeer.GetPattern(PatternInterface.Invoke));
        }
        finally { window.Close(); }
    });

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
