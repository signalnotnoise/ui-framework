using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

[TestClass]
public sealed class NativeHostTests
{
    [TestMethod]
    public void RetainsEditorAndUsesLatestUpdateWithOriginalRelease() => StaTestRunner.Run(() =>
    {
        var created = 0;
        var releases = 0;
        var unexpectedRelease = 0;
        var version = 1;
        TextBox? editor = null;
        using var host = new ViewHost(() => WpfUI.Native(
            () => { created++; return editor = new TextBox { Text = "native content", AcceptsReturn = true }; },
            text => text.Tag = version,
            version == 1 ? text => { Assert.IsNull(text.Parent); releases++; } : text => unexpectedRelease++).Id("editor"));
        TestVisualTree.Layout(host);
        editor!.Select(2, 4);
        version = 2;
        host.Refresh();
        Assert.AreEqual(1, created);
        Assert.AreEqual(2, editor.Tag);
        Assert.AreEqual("native content", editor.Text);
        Assert.IsTrue(editor.AcceptsReturn);
        Assert.AreEqual(4, editor.SelectionLength);
        host.Dispose();
        host.Dispose();
        Assert.AreEqual(1, releases);
        Assert.AreEqual(0, unexpectedRelease);
    });

    [TestMethod]
    public void KeyedReorderRetainsIslandsAndTypeChangeReplacesThem() => StaTestRunner.Run(() =>
    {
        var reverse = false;
        var replace = false;
        var created = 0;
        var released = 0;
        View Item(string key) => replace && key == "a"
            ? WpfUI.Native(() => new TreeView(), release: _ => released++).Id(key)
            : WpfUI.Native(() => { created++; return new TextBox { Text = key }; }, release: _ => released++).Id(key);
        using var host = new ViewHost(() => HStack((reverse ? new[] { "b", "a" } : new[] { "a", "b" }).Select(Item).ToArray()));
        TestVisualTree.Layout(host);
        var first = TestVisualTree.Find<TextBox>(host).First();
        reverse = true;
        host.Refresh();
        TestVisualTree.Layout(host);
        Assert.AreEqual(2, created);
        Assert.AreEqual(0, released);
        Assert.AreSame(first, TestVisualTree.Find<TextBox>(host).Last());
        replace = true;
        host.Refresh();
        TestVisualTree.Layout(host);
        Assert.AreEqual(1, released);
        Assert.AreEqual(1, TestVisualTree.Find<TreeView>(host).Count());
        host.Dispose();
        Assert.AreEqual(3, released);
    });

    [TestMethod]
    public void NativeGridChildrenAndAutomationNameRemainAppOwned() => StaTestRunner.Run(() =>
    {
        string? name = null;
        Grid? grid = null;
        using var host = new ViewHost(() => WpfUI.Native(() =>
        {
            grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            grid.Children.Add(new GridSplitter());
            AutomationProperties.SetName(grid, "factory name");
            return grid;
        }).AccessibilityLabel(name));
        Assert.AreEqual("factory name", AutomationProperties.GetName(grid!));
        name = "workspace";
        host.Refresh();
        Assert.AreEqual("workspace", AutomationProperties.GetName(grid!));
        Assert.AreEqual(1, grid!.Children.Count);
        Assert.AreEqual(new GridLength(2, GridUnitType.Star), grid.ColumnDefinitions[0].Width);
        name = null;
        host.Refresh();
        Assert.AreEqual(DependencyProperty.UnsetValue, grid.ReadLocalValue(AutomationProperties.NameProperty));
    });

    [TestMethod]
    public void ParentOwnedControlsAreRejectedWithoutReleaseOrReparenting() => StaTestRunner.Run(() =>
    {
        var input = new TextBox();
        var parent = new Border { Child = input };
        var releases = 0;
        Assert.ThrowsException<InvalidOperationException>(() => new ViewHost(() => WpfUI.Native(() => input, release: _ => releases++)));
        Assert.AreSame(input, parent.Child);
        Assert.AreEqual(0, releases);
        Assert.ThrowsException<InvalidOperationException>(() => new ViewHost(() => WpfUI.Native<TextBox>(() => null!)));
    });

    [TestMethod]
    public void InitialUpdateFailureReleasesAcceptedElementOnce() => StaTestRunner.Run(() =>
    {
        var releases = 0;
        TextBox? editor = null;
        Assert.ThrowsException<InvalidOperationException>(() => new ViewHost(() => WpfUI.Native(
            () => editor = new TextBox(), _ => throw new InvalidOperationException("update failed"),
            element => { Assert.IsNull(element.Parent); releases++; })));
        Assert.IsNotNull(editor);
        Assert.AreEqual(1, releases);
    });

    [TestMethod]
    public void InitialLayoutFailureAlsoReleasesNativeElement() => StaTestRunner.Run(() =>
    {
        var released = 0;
        Assert.ThrowsException<ArgumentException>(() => new ViewHost(() =>
            WpfUI.Native(() => new TextBox(), release: _ => released++).Padding(-1)));
        Assert.AreEqual(1, released);
    });

    [TestMethod]
    public void NavigationRecreatesIslandsFromApplicationState() => StaTestRunner.Run(() =>
    {
        var history = new NavigationStack<int>(0);
        var creates = 0;
        var releases = 0;
        var document = "saved in app model";
        using var host = new ViewHost(() => Navigation(history, route => route == 0
            ? WpfUI.Native(() => { creates++; return new TextBox { Text = document }; }, release: _ => releases++)
            : Text("details"), NavigationTransition.None));
        TestVisualTree.Layout(host);
        var initial = TestVisualTree.Find<TextBox>(host).Single();
        history.Push(1);
        TestVisualTree.Layout(host);
        Assert.AreEqual(1, releases);
        document = "updated while away";
        history.Back();
        TestVisualTree.Layout(host);
        var restored = TestVisualTree.Find<TextBox>(host).Single();
        Assert.AreNotSame(initial, restored);
        Assert.AreEqual(document, restored.Text);
        Assert.AreEqual(2, creates);
        host.Dispose();
        Assert.AreEqual(2, releases);
    });

    [TestMethod]
    public void ThrowingReleaseDoesNotSkipSiblingCleanup() => StaTestRunner.Run(() =>
    {
        var releases = 0;
        using var host = new ViewHost(() => HStack(
            WpfUI.Native(() => new TextBox(), release: _ => { releases++; throw new InvalidOperationException("cleanup"); }),
            WpfUI.Native(() => new TreeView(), release: _ => releases++)));
        Assert.ThrowsException<AggregateException>(host.Dispose);
        host.Dispose();
        Assert.AreEqual(2, releases);
    });

    [TestMethod]
    public void RemovalReleasesAndRemountCreatesFreshElement() => StaTestRunner.Run(() =>
    {
        var show = true;
        var creates = 0;
        var releases = 0;
        using var host = new ViewHost(() => VStack(show
            ? [WpfUI.Native(() => { creates++; return new RichTextBox(); }, release: _ => releases++).Id("console")]
            : []));
        show = false;
        host.Refresh();
        Assert.AreEqual(1, releases);
        show = true;
        host.Refresh();
        Assert.AreEqual(2, creates);
        host.Dispose();
        Assert.AreEqual(2, releases);
    });
}
