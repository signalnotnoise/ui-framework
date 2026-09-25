using Microsoft.VisualStudio.TestTools.UnitTesting;
using UI_Framework;
using UI_Framework.Wpf;

[TestClass]
public sealed class ViewValidationTests
{
    [TestMethod]
    public void AllDeclaredEnumValuesRemainRenderable() => StaTestRunner.Run(() =>
    {
        foreach (var kind in Enum.GetValues<ViewKind>())
        {
            var view = kind switch
            {
                ViewKind.Component => UI.Component<VirtualizationProbe>(),
                ViewKind.Platform => WpfUI.Native<System.Windows.Controls.TextBlock>(() => new()),
                ViewKind.Scroll => UI.Scroll(UI.Text("child")),
                ViewKind.Navigation => new View(kind) { Children = [UI.Text("screen").Id("screen")] },
                ViewKind.VirtualList => UI.VirtualList([UI.Text("row").Id("row")], 100),
                ViewKind.SplitPane => UI.SplitPane(UI.Text("first"), UI.Text("second"), new State<double>(100)),
                _ => new View(kind)
            };
            using var host = new ViewHost(() => view);
            Assert.IsNotNull(host.Content, kind.ToString());
        }
        foreach (var alignment in Enum.GetValues<ViewAlignment>())
        {
            using var host = new ViewHost(() => UI.Text("alignment").Align(alignment, alignment));
            Assert.IsNotNull(host.Content);
        }
        foreach (var appearance in Enum.GetValues<ButtonStyleKind>())
        {
            using var host = new ViewHost(() => UI.Button("appearance", () => { }).ButtonStyle(appearance));
            Assert.IsNotNull(host.Content);
        }
        foreach (var transition in Enum.GetValues<NavigationTransition>())
        {
            using var host = new ViewHost(() => new View(ViewKind.Navigation)
            {
                Children = [UI.Text("screen").Id("screen")], Transition = transition
            });
            Assert.IsNotNull(host.Content);
        }
    });

    [TestMethod]
    public void UnknownEnumsInNestedViewsPreserveTheRetainedTree() => StaTestRunner.Run(() =>
    {
        var view = UI.VStack(UI.Text("stable"));
        using var host = new ViewHost(() => view);
        var original = host.Content;
        foreach (var value in new[] { -1, int.MinValue, int.MaxValue, 999 })
        {
            View[] invalid =
            [
                UI.Text("invalid") with { Kind = (ViewKind)value },
                UI.Text("invalid") with { Horizontal = (ViewAlignment)value },
                UI.Text("invalid") with { Vertical = (ViewAlignment)value },
                UI.Text("invalid") with { ButtonAppearance = (ButtonStyleKind)value },
                UI.Text("invalid") with { Transition = (NavigationTransition)value }
            ];
            foreach (var child in invalid)
            {
                view = UI.VStack(child);
                Assert.ThrowsException<InvalidOperationException>(host.Refresh);
                Assert.AreSame(original, host.Content);
            }
        }
    });

    [TestMethod]
    public void MalformedDescriptionsFailBeforeMutatingRetainedTree() => StaTestRunner.Run(() =>
    {
        var view = UI.Text("stable");
        using var host = new ViewHost(() => view);
        var original = host.Content;
        View[] invalid =
        [
            view with { Gap = -1 }, view with { Inset = double.NaN },
            view with { DesiredWidth = double.PositiveInfinity }, view with { TextSize = 0 },
            view with { Kind = (ViewKind)999 }, view with { Kind = (ViewKind)Enum.GetValues<ViewKind>().Length },
            view with { Horizontal = (ViewAlignment)4 }, view with { Vertical = (ViewAlignment)4 },
            view with { ButtonAppearance = (ButtonStyleKind)3 }, view with { Transition = (NavigationTransition)2 },
            view with { Children = null! },
            view with { Children = [null!] }, view with { ComponentType = typeof(ThrowOnMounted) },
            view with { PlatformContent = new object() },
            new(ViewKind.AdaptiveGrid) { MinimumColumnWidth = 0 },
            new(ViewKind.AdaptiveGrid) { MinimumColumnWidth = double.NaN },
            new(ViewKind.TextEditor) { MaximumLength = -1 },
            new(ViewKind.TextEditor) { UndoHistoryLimit = -1 }
        ];
        foreach (var malformed in invalid)
        {
            view = malformed;
            Assert.ThrowsException<InvalidOperationException>(host.Refresh);
            Assert.AreSame(original, host.Content);
        }
        view = UI.Text("recovered");
        host.Refresh();
        TestVisualTree.Layout(host);
        Assert.AreEqual("recovered", TestVisualTree.Find<System.Windows.Controls.TextBlock>(host).Single().Text);
    });
}
