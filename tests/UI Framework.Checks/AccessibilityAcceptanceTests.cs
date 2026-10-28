using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

[TestClass]
public sealed class AccessibilityAcceptanceTests
{
    [TestMethod]
    public void VirtualListExposesDiscoveryAndRealizesOffscreenRowWithoutMountingEveryRow() => StaTestRunner.Run(() =>
    {
        using var host = new ViewHost(() => VirtualList(Enumerable.Range(0, 1000)
            .Select(i => Button($"Open {i}", () => { }).Id($"row-{i}").AccessibilityLabel($"Record {i}")), 200)
            .AccessibilityLabel("Records"));
        var window = new Window { Content = host, Width = 500, Height = 300, ShowInTaskbar = false };
        try
        {
            window.Show();
            TestVisualTree.Layout(host);
            TestAutomationTree.Initialize(window, "VirtualList");
            var list = TestVisualTree.Find<ItemsControl>(host).Single();
            Assert.IsTrue(TestVisualTree.Find<Button>(host).Count() < 100);
            var peer = UIElementAutomationPeer.CreatePeerForElement(list);
            Assert.IsNotNull(peer, "A virtual list needs its own peer, not only the realized children's peers.");
            Assert.AreEqual("Records", peer.GetName());
            var discovery = peer.GetPattern(PatternInterface.ItemContainer) as IItemContainerProvider;
            Assert.IsNotNull(discovery, "Unrealized rows must be discoverable through ItemContainer.");
            var target = discovery.FindItemByProperty(null!, AutomationElementIdentifiers.NameProperty.Id, "Record 999");
            Assert.IsNotNull(target);
            var realize = target.GetPatternProvider(VirtualizedItemPatternIdentifiers.Pattern.Id) as IVirtualizedItemProvider;
            Assert.IsNotNull(realize);
            realize.Realize();
            TestVisualTree.Layout(host);
            var last = TestVisualTree.Find<Button>(host).Single(button => Equals(button.Content, "Open 999"));
            Assert.AreEqual("Record 999", UIElementAutomationPeer.CreatePeerForElement(last)!.GetName());
            Assert.IsTrue(TestVisualTree.Find<Button>(host).Count() < 100, "Discovery must retain bounded native realization.");
        }
        finally { window.Close(); }
    });

    [TestMethod]
    public void VirtualDiscoveryUsesCurrentNamesAfterReorderRemovalAndKeyReuse() => StaTestRunner.Run(() =>
    {
        var ids = new StateList<int>(Enumerable.Range(0, 1000));
        var suffix = new State<string>("original");
        using var host = new ViewHost(() => VirtualList(ids.Select(i => Button($"Open {i}", () => { })
            .Id($"row-{i}").AccessibilityLabel($"Record {i} {suffix.Value}")), 200));
        var window = new Window { Content = host, Width = 500, Height = 300, ShowInTaskbar = false };
        try
        {
            window.Show();
            TestVisualTree.Layout(host);
            TestAutomationTree.Initialize(window, "VirtualList");
            var peer = UIElementAutomationPeer.CreatePeerForElement(TestVisualTree.Find<ItemsControl>(host).Single())!;
            var discovery = (IItemContainerProvider)peer.GetPattern(PatternInterface.ItemContainer)!;
            var property = AutomationElementIdentifiers.NameProperty.Id;
            Assert.IsNotNull(discovery.FindItemByProperty(null!, property, "Record 999 original"));
            suffix.Value = "renamed";
            ids.Move(999, 0);
            TestVisualTree.Layout(host);
            Assert.IsNull(discovery.FindItemByProperty(null!, property, "Record 999 original"));
            Assert.IsNotNull(discovery.FindItemByProperty(null!, property, "Record 999 renamed"));
            ids.Remove(999);
            TestVisualTree.Layout(host);
            Assert.IsNull(discovery.FindItemByProperty(null!, property, "Record 999 renamed"));
            ids.Add(999);
            TestVisualTree.Layout(host);
            var reinserted = discovery.FindItemByProperty(null!, property, "Record 999 renamed");
            Assert.IsNotNull(reinserted);
            ((IVirtualizedItemProvider)reinserted.GetPatternProvider(VirtualizedItemPatternIdentifiers.Pattern.Id)).Realize();
            TestVisualTree.Layout(host);
            Assert.IsTrue(TestVisualTree.Find<Button>(host).Any(button => Equals(button.Content, "Open 999")));
            Assert.IsTrue(TestVisualTree.Find<Button>(host).Count() < 100);
        }
        finally { window.Close(); }
    });

    [TestMethod]
    public void KeyboardTraversalSkipsDisabledControlsAndSurvivesThemeReplacement() => StaTestRunner.Run(() =>
    {
        var text = new State<string>("title");
        var check = new State<bool>(false);
        using var host = new ViewHost(() => VStack(Button("Before", () => { }),
            Button("Unavailable", () => { }).IsEnabled(false),
            TextField(text).AccessibilityLabel("Title"), Toggle("Remember", check), Button("After", () => { })));
        ThemeStyles.Apply(host, new ThemeTokens());
        var window = new Window { Content = host, Width = 500, Height = 500, ShowInTaskbar = false };
        try
        {
            window.Show();
            TestVisualTree.Layout(host);
            var buttons = TestVisualTree.Find<Button>(host).ToArray();
            var editor = TestVisualTree.Find<TextBox>(host).Single();
            var toggle = TestVisualTree.Find<CheckBox>(host).Single();
            Assert.IsTrue(buttons[0].Focus());
            Assert.IsTrue(buttons[0].MoveFocus(new TraversalRequest(FocusNavigationDirection.Next)));
            Assert.AreSame(editor, Keyboard.FocusedElement);
            Assert.IsTrue(editor.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next)));
            Assert.AreSame(toggle, Keyboard.FocusedElement);
            ThemeStyles.Apply(host, new ThemeTokens { Surface = "#202020", Ink = "#FFFFFF", Focus = "#FFFF00" });
            TestVisualTree.Layout(host);
            Assert.AreSame(toggle, Keyboard.FocusedElement);
            Assert.IsTrue(toggle.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next)));
            Assert.AreSame(buttons[2], Keyboard.FocusedElement);
            Assert.IsTrue(buttons[2].MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous)));
            Assert.AreSame(toggle, Keyboard.FocusedElement);
        }
        finally { window.Close(); }
    });

    [TestMethod]
    public void KeyboardTraversalReachesRowsBeyondTheInitialViewportInOrder() => StaTestRunner.Run(() =>
    {
        using var host = new ViewHost(() => VirtualList(Enumerable.Range(0, 1000)
            .Select(i => Button($"Open {i}", () => { }).Id($"row-{i}")), 200));
        var window = new Window { Content = host, Width = 500, Height = 300, ShowInTaskbar = false };
        try
        {
            window.Show();
            TestVisualTree.Layout(host);
            var current = TestVisualTree.Find<Button>(host).First();
            Assert.IsTrue(current.Focus());
            for (var i = 1; i <= 30; i++)
            {
                Assert.IsTrue(current.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next)));
                TestVisualTree.Layout(host);
                Assert.IsInstanceOfType<Button>(Keyboard.FocusedElement);
                current = (Button)Keyboard.FocusedElement;
                Assert.AreEqual($"Open {i}", current.Content, "Tab traversal must realize and reach the next logical row.");
            }
            for (var i = 29; i >= 0; i--)
            {
                Assert.IsTrue(current.MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous)));
                TestVisualTree.Layout(host);
                Assert.IsInstanceOfType<Button>(Keyboard.FocusedElement);
                current = (Button)Keyboard.FocusedElement;
                Assert.AreEqual($"Open {i}", current.Content);
            }
            Assert.IsTrue(TestVisualTree.Find<Button>(host).Count() < 100);
        }
        finally { window.Close(); }
    });

    [TestMethod]
    public void RetainedPeersReportCurrentEnabledReadOnlyAndSelectionStates() => StaTestRunner.Run(() =>
    {
        var text = new State<string>("title");
        var selection = new State<int>(0);
        var enabled = new State<bool>(true);
        using var host = new ViewHost(() => VStack(
            TextField(text).IsReadOnly(!enabled.Value).AccessibilityLabel("Title"),
            Picker(["First", "Second"], selection).AccessibilityLabel("Destination"),
            Button("Save", () => { }).IsEnabled(enabled.Value)));
        var window = new Window { Content = host, Width = 500, Height = 300, ShowInTaskbar = false };
        try
        {
            window.Show();
            TestVisualTree.Layout(host);
            InitializeAutomationTree(UIElementAutomationPeer.CreatePeerForElement(window)!);
            var editor = TestVisualTree.Find<TextBox>(host).Single();
            var editorPeer = UIElementAutomationPeer.CreatePeerForElement(editor)!;
            var value = (IValueProvider)editorPeer.GetPattern(PatternInterface.Value)!;
            var buttonPeer = UIElementAutomationPeer.CreatePeerForElement(TestVisualTree.Find<Button>(host).Single())!;
            var picker = TestVisualTree.Find<ComboBox>(host).Single();
            picker.IsDropDownOpen = true;
            TestVisualTree.Layout(host);
            var pickerPeer = UIElementAutomationPeer.CreatePeerForElement(picker)!;
            Assert.IsInstanceOfType<ISelectionProvider>(pickerPeer.GetPattern(PatternInterface.Selection));
            pickerPeer.ResetChildrenCache();
            var choices = pickerPeer.GetChildren()!;
            var first = (ISelectionItemProvider)choices.Single(child => child.GetName() == "First").GetPattern(PatternInterface.SelectionItem)!;
            var second = (ISelectionItemProvider)choices.Single(child => child.GetName() == "Second").GetPattern(PatternInterface.SelectionItem)!;
            Assert.IsTrue(first.IsSelected);
            Assert.IsFalse(second.IsSelected);
            Assert.IsFalse(value.IsReadOnly);
            Assert.IsTrue(buttonPeer.IsEnabled());
            enabled.Value = false;
            selection.Value = 1;
            TestVisualTree.Flush();
            Assert.AreSame(editor, TestVisualTree.Find<TextBox>(host).Single());
            Assert.IsTrue(value.IsReadOnly);
            Assert.IsFalse(buttonPeer.IsEnabled());
            Assert.IsFalse(first.IsSelected);
            Assert.IsTrue(second.IsSelected);
            Assert.ThrowsException<ElementNotEnabledException>(() => value.SetValue("rejected"));
            Assert.AreEqual("title", text.Value);
        }
        finally { window.Close(); }
    });

    private static void InitializeAutomationTree(AutomationPeer peer)
    {
        foreach (var child in peer.GetChildren() ?? []) InitializeAutomationTree(child);
    }
}
